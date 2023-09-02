using System.Text.Json;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Euctr;

public class EUCTRProcessor : IStudyProcessor
{
    public Study? ProcessData(string json_string, DateTime? download_datetime, ILoggingHelper _logging_helper)
    {
        ///////////////////////////////////////////////////////////////////////////////////////
        // Set up and deserialise string 
        ///////////////////////////////////////////////////////////////////////////////////////


        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        
        Euctr_Record? r = JsonSerializer.Deserialize<Euctr_Record?>(json_string, json_options);
        if (r is null)
        {
            _logging_helper.LogError(
                $"Unable to deserialise json file to Euctr_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }

        Study s = new();

        List<StudyIdentifier> identifiers = new();
        List<StudyTitle> titles = new();
        List<StudyOrganisation> organisations = new();
        List<StudyTopic> topics = new();
        List<StudyFeature> features = new();
        List<StudyCountry> countries = new();
        List<StudyCondition> conditions = new();
        List<StudyIEC> iec = new();

        List<DataObject> data_objects = new();
        List<ObjectTitle> object_titles = new();
        List<ObjectInstance> object_instances = new();
        List<ObjectDate> object_dates = new();
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Basics - id, status, type, start date 
        ///////////////////////////////////////////////////////////////////////////////////////

        string sid = r.sd_sid;

        if (string.IsNullOrEmpty(sid))
        {
            _logging_helper.LogError(
                $"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }
        
        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        // By definition with the EU CTR all studies are interventional trials. 
        // This already applied in download phase, but alert if not the case.
        
        s.study_type = r.study_type;
        s.study_type_id = s.study_type.GetTypeId();

        if (s.study_type_id != 11)
        {
            _logging_helper.LogLine($"Study type recorded as {s.study_type} - !!!unusual!!! - for {s.sd_sid}");
        }

        string? status = r.trial_status;
        if (!string.IsNullOrEmpty(status))
        {
            Tuple<int, string?> new_status = status switch
            {
                "Ongoing" => new Tuple<int, string?>(25, "Ongoing"),
                "Completed" => new Tuple<int, string?>(21, "Completed"),
                "Prematurely Ended" => new Tuple<int, string?>(22, "Terminated"),
                "Temporarily Halted" => new Tuple<int, string?>(18, "Suspended"),
                "Not Authorised" => new Tuple<int, string?>(11, "Withdrawn"),
                _ => new Tuple<int, string?>(0, null)
            };

            if (status == "Ongoing" && !string.IsNullOrEmpty(r.recruitment_status))
            {
                // Largely empty at the moment - possible values collected, 
                // to distinguish (later) between pre, during and post recruitment
                
                _logging_helper.LogLine($"For info: Study recruitment status given as {r.recruitment_status} for {s.sd_sid}");
            }

            s.study_status_id = new_status.Item1;
            s.study_status = new_status.Item2;
        }

        // Study start_date may be in ISO yyyy-MM-dd format or EU dd/MM/yyyy format.

        SplitDate? start = r.start_date?.GetDatePartsFromEuropeanString();
        if (start is not null)
        {
            s.study_start_year = start.year;
            s.study_start_month = start.month;
        }
        

        ///////////////////////////////////////////////////////////////////////////////////////
        // Study contributors
        ///////////////////////////////////////////////////////////////////////////////////////

        string? sponsor_name = "No organisation name provided in source data";    // initial default
        string? sponsor = r.sponsor_name;
        if (sponsor.IsNotPlaceHolder() && sponsor.AppearsGenuineOrgName())
        {
            sponsor_name = sponsor?.TidyOrgName(sid).StandardisePharmaName();
            string? lc_sponsor = sponsor_name?.ToLower();
            if (!string.IsNullOrEmpty(lc_sponsor) && lc_sponsor.Length > 1
                && lc_sponsor != "dr" && lc_sponsor != "no profit")
            {
                organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", null, sponsor_name));
            }
        }

        // May get funders or other supporting organisations.

        var funders = r.organisations;
        if (funders?.Any() is true)
        {
            foreach (EMAOrganisation org in funders)
            {
                string? org_n = org.org_name;
                if (!string.IsNullOrEmpty(org_n) 
                    && org_n.IsNotPlaceHolder() && org_n.AppearsGenuineOrgName())
                {
                    // Situation where the same organisation is the sponsor and funder
                    // now resolved in the Coding module. insert as whatever listed as -
                    // usually a funder, unless already listed as a sponsor with the same name

                    string? org_name = org_n.TidyOrgName(sid).StandardisePharmaName();
                    string lc_orgn = org_name!.ToLower();
                    if (lc_orgn.Length > 1 && lc_orgn != "dr" && lc_orgn != "no profit")
                    {
                        if (org.org_role_id != 54 ||
                           (org.org_role_id == 54 && org_name.IsNotInOrgsAsRoleAlready(54, organisations)))
                        {
                            organisations.Add(new StudyOrganisation(sid, org.org_role_id, org.org_role, null,
                                org_name));
                        }
                    }
                }
            }
        }

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study identifiers
        ///////////////////////////////////////////////////////////////////////////////////////

        if (r.identifiers?.Any() == true)
        {
            foreach (Identifier ident in r.identifiers)
            {
                if (ident.identifier_value is not null)
                {
                    // Need to check for 'nil' values 
                    
                    bool add_id = true;
                    string ident_lc = ident.identifier_value.ToLower();
                    if (ident_lc is "pending" or "nd" or "na" or "n/a" or "n.a."
                        or "none" or "n/a." or "no" or "none" or "pending")
                    {
                        add_id = false;
                    }
                    if (ident_lc.StartsWith("not ") || ident_lc.StartsWith("to be ")
                        || ident_lc.StartsWith("not-") || ident_lc.StartsWith("not_")
                        || ident_lc.StartsWith("notapplic") || ident_lc.StartsWith("notavail")
                        || ident_lc.StartsWith("tobealloc") || ident_lc.StartsWith("tobeapp"))
                    {
                        add_id = false;
                    }
                    if (ident_lc is "n.a" or "in progress" or "applied for" 
                        or "non applicable" or "none available" or "applying for" 
                        or "being applied" or "to follow")
                    {
                        add_id = false;
                    }
   
                    if (add_id)
                    {
                        string? ident_org = ident.identifier_org?.TidyOrgName(sid).StandardisePharmaName();
                        identifiers.Add(new StudyIdentifier(sid, ident.identifier_value, ident.identifier_type_id,
                            ident.identifier_type, ident.identifier_org_id, ident_org));
                    }
                }
            }
        }
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study titles
        ///////////////////////////////////////////////////////////////////////////////////////
        
        // May need to sort out which is which, especially with 'scientific acronyms'.
        
        string? public_title = r.public_title;
        string? sci_title = r.scientific_title;
        string? acro = r.acronym;
        string? sci_acro = r.scientific_acronym;
        
        // not uncommon situations
         
        if (string.IsNullOrEmpty(acro) && !string.IsNullOrEmpty(sci_acro))
        {
            acro = sci_acro;
        }

        if (string.IsNullOrEmpty(public_title) && !string.IsNullOrEmpty(acro) && acro.Length > 20)
        {
            public_title = acro;
        }
        
        if (string.IsNullOrEmpty(public_title) && !string.IsNullOrEmpty(sci_acro) && sci_acro.Length > 20)
        {
            public_title = sci_acro;
        }

        if (!string.IsNullOrEmpty(acro) && acro.Length <= 20)
        {
            string acro_lc = acro.ToLower();
            if (acro_lc.EndsWith(" trial"))
            {
                acro = acro[..acro_lc.LastIndexOf(" trial", StringComparison.Ordinal)];
            }

            if (acro_lc.EndsWith(" study"))
            {
                acro = acro[..acro_lc.LastIndexOf(" study", StringComparison.Ordinal)];
            }

            if (acro_lc.StartsWith("the "))
            {
                acro = acro[4..];
            }
        }

        if (!string.IsNullOrEmpty(acro) && acro.Length > 20 && !string.IsNullOrEmpty(public_title))
        {
            // can a 'real' acronym be extracted from the long acronym?
            if (acro.Contains(':'))
            {
                int colon_pos = acro.IndexOf(':');
                acro = acro[..colon_pos];
            }
        }
        
        bool default_title_identified = false;
        if (!string.IsNullOrEmpty(public_title) && public_title.IsNotPlaceHolder())
        {
            public_title = public_title.LineClean()!;
            titles.Add(new StudyTitle(sid, public_title, 15, "Registry public title", "en",
                                           11, true, "From the EU CTR"));
            s.display_title = public_title;
            default_title_identified = true;
        }

        
        if (!string.IsNullOrEmpty(sci_title) && sci_title.IsNotPlaceHolder())
        {
            if (!sci_title.NameAlreadyPresent(titles))
            {
                sci_title = sci_title.LineClean();
                titles.Add(new StudyTitle(sid, sci_title, 16, "Registry scientific title", "en",
                                            11, !default_title_identified, "From the EU CTR"));
                if (string.IsNullOrEmpty(s.display_title ))
                {
                    s.display_title = sci_title;
                }
                default_title_identified = true;
            }
        }

        if (!string.IsNullOrEmpty(acro) && acro.IsNotPlaceHolder())
        {
            if (!acro.NameAlreadyPresent(titles))
            {
                acro = acro.LineClean();
                string? acro_lc = acro?.ToLower();
                if (acro_lc is not null && 
                    !acro_lc.StartsWith("not ") && !acro_lc.StartsWith("non ") && acro_lc != "none"
                    && acro_lc.Length > 2 && acro_lc != "n/a" && acro_lc != "n.a."
                    && !acro_lc.StartsWith("no ap") && !acro_lc.StartsWith("no av"))
                {
                    titles.Add(new StudyTitle(sid, acro, 14, "Acronym or Abbreviation", "en",
                                                11, !default_title_identified, "From the EU CTR"));
                    if (string.IsNullOrEmpty(s.display_title ))
                    {
                        s.display_title = acro;
                    }
                    default_title_identified = true;
                }
            }
        }
        
        if (!string.IsNullOrEmpty(sci_acro) && sci_acro.IsNotPlaceHolder())
        {
            if (!sci_acro.NameAlreadyPresent(titles))
            {
                sci_acro = sci_acro.LineClean();
                string? acro_lc = sci_acro?.ToLower();
                if (acro_lc is not null && 
                    !acro_lc.StartsWith("not ") && !acro_lc.StartsWith("non ") && acro_lc != "none"
                    && acro_lc.Length > 2 && acro_lc != "n/a" && acro_lc != "n.a."
                    && !acro_lc.StartsWith("no ap") && !acro_lc.StartsWith("no av"))
                {
                    titles.Add(new StudyTitle(sid, sci_acro, 14, "Acronym or Abbreviation", "en",
                        11, !default_title_identified, "From the EU CTR"));
                    if (string.IsNullOrEmpty(s.display_title ))
                    {
                        s.display_title = acro;
                    }
                }
            }
        }
        
        // Add in an explanatory message... if still no title (!) -
        // there are a few early trials in EUCTR where this is the case

        if (string.IsNullOrEmpty(s.display_title))
        {
            s.display_title = sid + " (No meaningful title provided)";
        }

        // Finally, truncate display_title if too long - some 
        // titles are extremely long...

        if (s.display_title!.Length > 400)
        {
            s.display_title = s.display_title[..400] + "...";
        }

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study description
        ///////////////////////////////////////////////////////////////////////////////////////
        
        string? objs = r.primary_objectives;
        if (objs is not null && objs.Length >= 16 
            && !objs.ToLower().StartsWith("see ") && !objs.ToLower().StartsWith("not "))
        {
            string? clean_objs = objs.FullClean();
            if (clean_objs is not null)
            {
                string study_objectives = !clean_objs.ToLower().StartsWith("primary") 
                                          && !clean_objs.ToLower().StartsWith("main ")
                    ? "Primary objectives: " + clean_objs
                    : clean_objs;
                s.brief_description = study_objectives;
            }
        }
        
        string? end_points = r.primary_endpoints;
        if (end_points is not null && end_points.Length >= 16 &&
            !end_points.ToLower().StartsWith("see ") &&
            !end_points.ToLower().StartsWith("not ") &&
            !string.Equals(end_points, objs, StringComparison.CurrentCultureIgnoreCase))
        {
            string? clean_eps = end_points.FullClean();
            if (clean_eps is not null)
            {
                string study_endpoints = !clean_eps.ToLower().StartsWith("primary")
                    ? "Primary endpoints: " + clean_eps
                    : clean_eps;

                s.brief_description += string.IsNullOrEmpty(s.brief_description)
                    ? study_endpoints
                    : "\n" + study_endpoints;
            }
        }
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study features
        ///////////////////////////////////////////////////////////////////////////////////////

        if (r.features?.Any() == true)
        {
            foreach(EMAFeature f in r.features)
            {
                features.Add(new StudyFeature(sid, f.feature_id, f.feature_name,
                    f.feature_value_id, f.feature_value_name));
            }
        }

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study conditions
        ///////////////////////////////////////////////////////////////////////////////////////

        string? named_condition = r.medical_condition;
        if (!string.IsNullOrEmpty(named_condition)      // avoid the long complex ones, which are not conditions
            && !named_condition.Contains('\r') && !named_condition.Contains('\n') 
            && named_condition.Length < 200)
        {
            conditions.Add(new StudyCondition(sid, r.medical_condition, null, null, null));
        }

        // MedDRA version and level details not used
        // Term captured for possible MESH / ICD equivalence.

        var meddra_terms = r.meddra_terms;
        if (meddra_terms?.Any() is true)
        {
            foreach (var t in meddra_terms)
            {
                if (t.term is not null)
                {
                    conditions.Add(new StudyCondition(sid, t.term, 16, "MedDRA", t.code));
                }
            }
        }

        // Condition objects in file
        
        if (r.conditions?.Any() == true)
        {
            foreach (var c in r.conditions)
            {
                if (c.condition_name is not null && c.condition_name.IsNotInConditionsAlready(conditions))
                {
                    conditions.Add(new StudyCondition(sid, c.condition_name, c.condition_ct_id,
                        c.condition_ct, c.condition_ct_code));
                }
            }
        }

        if (conditions.Any())
        {
            conditions = conditions.RemoveNonInformativeConditions();
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Inclusion / Exclusion criteria
        ///////////////////////////////////////////////////////////////////////////////////////

        int study_iec_type = 0;
        int num_inc_criteria = 0;
        string? ic = r.inclusion_criteria;
        if (!string.IsNullOrEmpty(ic))
        {
            List<Criterion>? crits = IECFunctions.GetNumberedCriteria(sid, ic, "inclusion");
            if (crits is not null)
            {
                int seq_num = 0;
                foreach (Criterion cr in crits)
                {    
                    seq_num++;
                    iec.Add(new StudyIEC(sid, seq_num, cr.CritTypeId, cr.CritType,
                        cr.SplitType, cr.Leader, cr.IndentLevel, cr.LevelSeqNum, cr.SequenceString, cr.CritText));
                }
                study_iec_type = (crits.Count == 1) ? 2 : 4;
                num_inc_criteria = crits.Count;
            }
        }

        string? ec = r.exclusion_criteria;
        if (!string.IsNullOrEmpty(ec))
        {
            List<Criterion>? crits = IECFunctions.GetNumberedCriteria(sid, ec, "exclusion");
            if (crits is not null)
            {
                int seq_num = num_inc_criteria;
                foreach (Criterion cr in crits)
                {
                    seq_num++;
                    iec.Add(new StudyIEC(sid, seq_num, cr.CritTypeId, cr.CritType,
                        cr.SplitType, cr.Leader, cr.IndentLevel, cr.LevelSeqNum, cr.SequenceString, cr.CritText));
                }
                study_iec_type += (crits.Count == 1) ? 5 : 6;
            }
        }
               
        s.iec_level = study_iec_type;
        

        ///////////////////////////////////////////////////////////////////////////////////////
        // Eligibility
        ///////////////////////////////////////////////////////////////////////////////////////

        s.study_gender_elig = r.gender;
        s.study_gender_elig_id = r.gender.GetGenderEligId();

        if (r.minage is not null)
        {
            if (r.minage.Contains('('))    // Not years - need to extract units.
            {
                int left_bracket_pos = r.minage.IndexOf('(');
                string min_age_num = r.minage[..left_bracket_pos].Trim();
                if (int.TryParse(min_age_num, out int age))
                {
                    s.min_age = age;
                }
                string units = r.minage[left_bracket_pos..].Trim('(', ')', ' ').ToLower();
                if (units == "days")
                {
                    s.min_age_units = "Days";
                }
                if (units == "weeks")
                {
                    s.min_age_units = "Weeks";
                }
                if (units == "months")
                {
                    s.min_age_units = "Months";
                }
            }
            else
            {
                if (int.TryParse(r.minage, out int age))
                {
                    s.min_age = age;
                    s.min_age_units = "Years";
                }
            } 
            s.min_age_units_id = s.min_age_units.GetTimeUnitsId();
        }
        
        if (r.maxage is not null)
        {
            if (r.maxage.Contains('('))    // Not years - need to extract units.
            {
                int left_bracket_pos = r.maxage.IndexOf('(');
                string max_age_num = r.maxage[..left_bracket_pos].Trim();
                if (int.TryParse(max_age_num, out int age))
                {
                    s.max_age = age;
                }
                string units = r.maxage[left_bracket_pos..].Trim('(', ')', ' ').ToLower();
                if (units == "days")
                {
                    s.max_age_units = "Days";
                }
                if (units == "weeks")
                {
                    s.max_age_units = "Weeks";
                }
                if (units == "months")
                {
                    s.max_age_units = "Months";
                }
            }
            else
            {
                if (int.TryParse(r.maxage, out int age))
                {
                    s.max_age = age;
                    s.max_age_units = "Years";
                    s.max_age_units_id = 17;
                }
            }
            s.max_age_units_id = s.max_age_units.GetTimeUnitsId();
        }
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Topics ( = IMPs listed)
        ///////////////////////////////////////////////////////////////////////////////////////

        if (r.imp_topics?.Any() == true)
        {
            foreach (EMAImp i in r.imp_topics)
            {
                // Get name from product name, trade name or inn - in that order
                string? imp_name = i.product_name;
                if (string.IsNullOrEmpty(imp_name) && !string.IsNullOrEmpty(i.trade_name))
                {
                    imp_name = i.trade_name;
                }
                if (string.IsNullOrEmpty(imp_name) && !string.IsNullOrEmpty(i.inn))
                {
                    imp_name = i.inn;
                }

                if (imp_name is not null && imp_name.IsNotInTopicsAlready(topics))
                {
                    topics.Add(!string.IsNullOrEmpty(i.cas_number)
                        ? new StudyTopic(sid, 12, "chemical / agent", imp_name, "CAS", 23, i.cas_number)
                        : new StudyTopic(sid, 12, "chemical / agent", imp_name));
                }
            }

            topics = topics.RemoveNonInformativeTopics();
        }
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Countries
        ///////////////////////////////////////////////////////////////////////////////////////
        
        if (r.countries?.Any() is true)
        {
            foreach ( EMACountry cline in r.countries)
            {
                string? country_name = cline.country_name?.LineClean();
                string? country_status = cline.status?.Trim();
                if (country_name is not null)
                {
                    country_name = country_name.Replace("Korea, Republic of", "South Korea");
                    country_name = country_name.Replace("Russian Federation", "Russia");
                    country_name = country_name.Replace("Tanzania, United Republic of", "Tanzania");

                    if (string.IsNullOrEmpty(country_status))
                    {
                        countries.Add(new StudyCountry(sid, country_name));
                    }
                    else
                    {
                        int? status_id = country_status.GetStatusId();
                        countries.Add(new StudyCountry(sid, country_name, status_id, country_status));
                    }
                }
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////
        // Registry entry data object
        ///////////////////////////////////////////////////////////////////////////////////////

        string object_title = "EU CTR registry entry";
        string object_display_title = s.display_title + " :: EU CTR registry entry";
        SplitDate? entered_in_db = null;
        if (!string.IsNullOrEmpty(r.date_registration))
        {
            entered_in_db = r.date_registration.GetDatePartsFromEuropeanString();
        }


        int? registry_pub_year = (entered_in_db is not null) ? entered_in_db.year : s.study_start_year;
        string sd_oid = sid + " :: 13 :: " + object_title;

        data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, registry_pub_year,
            23, "Text", 13, "Trial Registry entry", 100123, "EU Clinical Trials Register",
            12, download_datetime));

        // Data object title is the single display title.
        
        object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22, 
                              "Study short name :: object type", true));

        // Date of registry entry.
        
        if (entered_in_db != null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                      entered_in_db.year, entered_in_db.month, entered_in_db.day, entered_in_db.date_string));
        }

        // Instance url
        
        string details_url = r.details_url!;       // cannot be null, else there would be no data!
        object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                             details_url, true, 35, "Web text"));

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Results entry data object
        ///////////////////////////////////////////////////////////////////////////////////////

        string? results_url = r.results_url;
        if (!string.IsNullOrEmpty(results_url))
        {
            object_title = "EU CTR results entry";
            object_display_title = s.display_title + " :: EU CTR results entry";
            sd_oid = sid + " :: 28 :: " + object_title;

            // Get the date data if available.

            string? results_first_date = r.results_date_posted;
            string? results_revision_date = r.results_revision_date;
            SplitDate? results_date = null;
            SplitDate? results_revision = null;
            int? results_pub_year = null;
            if (!string.IsNullOrEmpty(results_first_date))
            {
                results_date = results_first_date.GetDatePartsFromEuropeanString();
                results_pub_year = results_date?.year;
            }

            if (!string.IsNullOrEmpty(results_revision_date))
            {
                results_revision = results_revision_date.GetDatePartsFromEuropeanString();
            }

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_pub_year,
                            23, "Text", 28, "Trial registry results summary", 100123,
                           "EU Clinical Trials Register", 12, download_datetime));

            // Data object title is the single display title.
            
            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                            22, "Study short name :: object type", true));

            // Instance url 
            
            object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                            results_url, true, 35, "Web text"));

            // Dates
            
            if (results_date is not null)
            {
                object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                    results_date.year, results_date.month, results_date.day, results_date.date_string));
            }
            if (results_revision is not null)
            {
                object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                    results_revision.year, results_revision.month, results_revision.day, results_revision.date_string));
            }

            
            ////////////////////////////////////////////////////////////////////////////////////////
            // CSR data object
            ///////////////////////////////////////////////////////////////////////////////////////
            
            // If there is a reference to a CSR pdf to download include that.
            // Seems to be on the web pages in two forms.

            // Form A 

            string? results_summary_link = r.results_summary_link;

            if (!string.IsNullOrEmpty(results_summary_link))
            {
                string? results_summary_name = r.results_summary_name;
                int title_type_id;
                string title_type;
                bool add_record = true;

                if (!string.IsNullOrEmpty(results_summary_name))
                {
                    string title_to_check = results_summary_name.ToLower();

                    // Don't add if the name implies a reference to a clinical trials.gov 
                    // summary results records - over 700 do...

                    if (title_to_check.Contains("ctg") || title_to_check.Contains("ct_g") ||
                        title_to_check.Contains("ct.g") || title_to_check.Contains("clinicaltrials.gov"))
                    {
                        add_record = false;
                    }

                    object_title = results_summary_name;
                    object_display_title = s.display_title + " :: " + results_summary_name;
                    title_type_id = 21;
                    title_type = "Study short name :: object name";
                }
                else
                {
                    object_title = "CSR summary";
                    object_display_title = s.display_title + " :: CSR summary";
                    title_type_id = 22;
                    title_type = "Study short name :: object type";
                }

                if (add_record)
                {
                    sd_oid = sid + " :: 79 :: " + object_title;

                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_date?.year,
                              23, "Text", 79, "CSR summary", null, sponsor_name, 11, download_datetime));

                    // Data object title is the single display title.
                    
                    object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                              title_type_id, title_type, true));

                    // instance url 
                    object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                               results_summary_link, true, 11, "PDF"));
                }
            }

            // Form B

            string? results_pdf_link = r.results_pdf_link;

            if (!string.IsNullOrEmpty(results_pdf_link))
            {
                object_title = "CSR summary - PDF DL";
                object_display_title = s.display_title + " :: CSR summary";
                int title_type_id = 22;
                string title_type = "Study short name :: object type";

                sd_oid = sid + " :: 79 :: " + object_title;

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_date?.year,
                    23, "Text", 79, "CSR summary", null, sponsor_name, 11, download_datetime));

                // Data object title is the single display title.
                
                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                    title_type_id, title_type, true));

                // Instance url
                
                object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                    results_pdf_link, true, 11, "PDF"));

            }
        }

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Construct final study object
        ///////////////////////////////////////////////////////////////////////////////////////

        s.identifiers = identifiers;
        s.titles = titles;
        s.organisations = organisations;
        s.topics = topics;
        s.features = features;
        s.countries = countries;
        s.conditions = conditions;
        s.iec = iec;

        s.data_objects = data_objects;
        s.object_titles = object_titles;
        s.object_instances = object_instances;
        s.object_dates = object_dates;

        return s;
    }
}






