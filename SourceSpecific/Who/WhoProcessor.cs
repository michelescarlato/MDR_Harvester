using System.Text.Json;
using System.Text.RegularExpressions;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Who;

public class WHOProcessor : IStudyProcessor
{
    ILoggingHelper _logger_helper;

    public WHOProcessor(ILoggingHelper logger_helper)
    {
        _logger_helper = logger_helper;
    }

    public Study? ProcessData(string json_string, DateTime? download_datetime)
    {
        // set up json reader and deserialise file to a WHO record object.

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        Who_Record? r = JsonSerializer.Deserialize<Who_Record?>(json_string, json_options);
        if (r is null)
        {
            _logger_helper.LogError($"Unable to deserialise json file to Who_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }

        Study s = new();

        // get date retrieved in object fetch
        // transfer to study and data object records

        List<StudyIdentifier> study_identifiers = new();
        List<StudyTitle> study_titles = new();
        List<StudyFeature> study_features = new();
        List<StudyContributor> study_contributors = new();
        List<StudyCountry> countries = new();
        List<StudyCondition> conditions = new();
        List<StudyIEC> iec = new();

        List<DataObject> data_objects = new();
        List<ObjectTitle> data_object_titles = new();
        List<ObjectDate> data_object_dates = new();
        List<ObjectInstance> data_object_instances = new();

        WhoHelpers wh = new();

        string? sid = r.sd_sid;

        if (string.IsNullOrEmpty(sid))
        {
            _logger_helper.LogError($"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        int? source_id = r.source_id;
        string? source_name = wh.GetSourceName(source_id);

        // Do inital identifier representing the registry id.

        SplitDate? registration_date = null;
        //string? date_registration = r.date_registration;  
        if (!string.IsNullOrEmpty(r.date_registration))
        {
            registration_date = r.date_registration.GetDatePartsFromISOString();
        }

        study_identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", source_id,
                                    source_name, registration_date?.date_string, null));

        // Obtain piublic and scientific titles. In some cases these are acronyms.

        string? public_title = r.public_title;
        bool public_title_present = public_title.AppearsGenuineTitle();
        if (public_title_present)
        {
            public_title = public_title.ReplaceApos();
        }

        string? scientific_title = r.scientific_title;
        bool scientific_title_present = scientific_title.AppearsGenuineTitle();
        if (scientific_title_present)
        {
            scientific_title = scientific_title.ReplaceApos();
        }
        string source_string = "From the " + source_name;


        if (!public_title_present)
        {
            // No public title - use scientific title as default.

            if (scientific_title_present)
            {
                if (scientific_title!.Length < 11)
                {
                    study_titles.Add(new StudyTitle(sid, scientific_title, 14, "Acronym or Abbreviation", true, source_string));
                }
                else
                {
                    study_titles.Add(new StudyTitle(sid, scientific_title, 16, "Registry scientific title", true, source_string));
                }

                s.display_title = scientific_title;
            }
            else
            {
                s.display_title = "No public or scientific title provided";
            }
        }
        else
        {
            // Public title available 

            if (public_title!.Length < 11)
            {
                study_titles.Add(new StudyTitle(sid, public_title, 14, "Acronym or Abbreviation", true, source_string));
            }
            else
            {
                study_titles.Add(new StudyTitle(sid, public_title, 15, "Registry public title", true, source_string));
            }

            if (scientific_title_present && scientific_title!.ToLower() != public_title!.ToLower())
            {
                study_titles.Add(new StudyTitle(sid, scientific_title, 16, "Registry scientific title", false, source_string));
            }

            s.display_title = public_title;
        }

        // English used as the default title language.
        // Need a mechanism, here to try and identify at least major language variations
        // e.g. Spanish, German, French - may be linkable to the source registry

        s.title_lang_code = "en";  

        // Brief description

        string? interventions = r.interventions;
        if (!string.IsNullOrEmpty(interventions))
        {
            if (!interventions.ToLower().Contains("not applicable") && !interventions.ToLower().Contains("not selected")
                && !(interventions.ToLower() == "n/a") && !(interventions.ToLower() == "na"))
            {
                interventions = interventions?.StringClean();
                if (!interventions!.ToLower().StartsWith("intervention"))
                {
                    interventions = "Interventions: " + interventions;
                }
                s.brief_description = interventions;
            }
        }


        string? primary_outcome = r.primary_outcome;
        if (!string.IsNullOrEmpty(primary_outcome))
        {
            if (!primary_outcome.ToLower().Contains("not applicable") && !primary_outcome.ToLower().Contains("not selected")
                && !(primary_outcome.ToLower() == "n/a") && !(primary_outcome.ToLower() == "na"))
            {
                primary_outcome = primary_outcome?.StringClean();
                if (!primary_outcome!.ToLower().StartsWith("primary"))
                {
                    primary_outcome = "Primary outcome(s): " + primary_outcome;
                }
                s.brief_description += string.IsNullOrEmpty(s.brief_description) ? primary_outcome : "\n" + primary_outcome;
            }
        }


        string? design_string = r.design_string;
        if (!string.IsNullOrEmpty(design_string))
        {
            if (!design_string.ToLower().Contains("not applicable") && !design_string.ToLower().Contains("not selected")
                && !(design_string.ToLower() == "n/a") && !(design_string.ToLower() == "na"))
            {
                design_string = design_string?.StringClean();
                if (!design_string!.ToLower().StartsWith("primary"))
                {
                    design_string = "Study Design: " + design_string;
                }
                s.brief_description += string.IsNullOrEmpty(s.brief_description) ? design_string : "\n" + design_string;
            }
        }


        // data sharing statement

        string? ipd_plan = r.ipd_plan;
        if (!string.IsNullOrEmpty(ipd_plan)
            && ipd_plan.Length > 10
            && ipd_plan.ToLower() != "not available"
            && ipd_plan.ToLower() != "not avavilable"
            && ipd_plan.ToLower() != "not applicable"
            && !ipd_plan.Contains("justification or reason for"))
        {
            ipd_plan = ipd_plan.StringClean();
            s.data_sharing_statement = ipd_plan; 
        }
        
        string? ipd_description = r.ipd_description;
        if (!string.IsNullOrEmpty(ipd_description)
            && ipd_description.Length > 10
            && ipd_description.ToLower() != "not available"
            && ipd_description.ToLower() != "not avavilable"
            && ipd_description.ToLower() != "not applicable"
            && !ipd_description.Contains("justification or reason for"))
        {
            ipd_description = ipd_description.StringClean();
            s.data_sharing_statement += string.IsNullOrEmpty(s.data_sharing_statement) ? ipd_description : "\n" + ipd_description;
        }


        // Study basics.

        string? date_enrolment = r.date_enrolment;
        if (!string.IsNullOrEmpty(date_enrolment))
        {
            SplitDate? enrolment_date = date_enrolment.GetDatePartsFromISOString();
            if (enrolment_date is not null && enrolment_date.year > 1960)
            {
                s.study_start_year = enrolment_date.year;
                s.study_start_month = enrolment_date.month;
            }
        }


        // Study type and status. 

        string? study_type = r.study_type;
        string? study_status = r.study_status;

        if (!string.IsNullOrEmpty(study_type))
        {
            if (study_type.StartsWith("Other"))
            {
                s.study_type = "Other";
                s.study_type_id = 16;
            }
            else
            {
                s.study_type = study_type; ;
                s.study_type_id = s.study_type.GetTypeId();
            }
        }
        else
        {
            s.study_type = "Not yet known";
            s.study_type_id = 0;
        }

        if (!string.IsNullOrEmpty(study_status))
        {
            if (study_status.StartsWith("Other"))
            {
                s.study_status = "Other";
                s.study_status_id = 24;
            }
            else
            {
                s.study_status = study_status;
                s.study_status_id = s.study_status.GetStatusId();
            }
        }
        else
        {
            s.study_status = "Unknown status";
            s.study_status_id = 0;
        }


        // enrolment targets, gender and age groups
        // use actual enrolment figure if present and not an ISO date or a dummy figure
        // but only if the status of the trial is 'completed'.
        // Otherwise use the target if that is all that is available (it usually is)

        string? enrolment = null;
        if (study_status is not null && study_status.ToLower() == "completed")
        {
            string? results_actual_enrollment = r.results_actual_enrollment;
            if (!string.IsNullOrEmpty(results_actual_enrollment)
                && !results_actual_enrollment.Contains("9999")
                && !Regex.Match(results_actual_enrollment, @"\d{4}-\d{2}-\d{2}").Success)
            {
                enrolment = results_actual_enrollment;
            }
        }

        if (enrolment is null)
        {
            string? target_size = r.target_size;
            if (!string.IsNullOrEmpty(target_size)
                && !target_size.Contains("9999"))
            {
                enrolment = target_size;
            }
        }

        s.study_enrolment = enrolment;


        string? agemin = r.agemin;
        string? agemin_units = r.agemin_units;
        string? agemax = r.agemax;
        string? agemax_units = r.agemax_units;

        if (agemin is not null && int.TryParse(agemin, out int min))
        {
            s.min_age = min;
            if (agemin_units is not null && agemin_units.StartsWith("Other"))
            {
                // was not classified previously...
                s.min_age_units = agemin_units.GetTimeUnits();
            }
            else
            {
                s.min_age_units = agemin_units;
            }
            s.min_age_units_id = s.min_age_units.GetTimeUnitsId();
        }


        if (agemax is not null && Int32.TryParse(agemax, out int max))
        {
            if (max != 0)
            {
                s.max_age = max;
                if (agemax_units is not null && agemax_units.StartsWith("Other"))
                {
                    // was not classified previously...
                    s.max_age_units = agemax_units.GetTimeUnits();
                }
                else
                {
                    s.max_age_units = agemax_units;
                }
                s.max_age_units_id = s.max_age_units.GetTimeUnitsId();
            }
        }

        string? gender = r.gender;
        if (gender is not null && gender.Contains("?? Unable to classify"))
        {
            gender = "Not provided";
        }
        s.study_gender_elig = gender;
        s.study_gender_elig_id = gender.GetGenderEligId();


        // Add study attribute records.
        // beginning with study contributors and funders.

        string? sponsor_name = "No organisation name provided in source data";
        bool? sponsor_is_org = null;

        string? primary_sponsor = r.primary_sponsor;
        if (!string.IsNullOrEmpty(primary_sponsor))
        {
            sponsor_name = primary_sponsor.TidyOrgName(sid);
            if (sponsor_name is not null && sponsor_name.AppearsGenuineOrgName())
            {
                if ((sponsor_name.ToLower().StartsWith("dr ") || sponsor_name.ToLower().StartsWith("prof ")
                  || sponsor_name.ToLower().StartsWith("professor ")))
                {
                    sponsor_is_org = false;
                    study_contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", sponsor_name, null, null));
                }
                else
                {
                    sponsor_is_org = true;
                    study_contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", null, sponsor_name));
                }
            }
        }
        
        string? funders = r.source_support;
        if (!string.IsNullOrEmpty(funders))
        {
            string[] funder_names = funders.Split(";");  // can have multiple names separated by semi-colons
            if (funder_names?.Any() == true)
            {
                foreach (string funder in funder_names)
                {
                    string? funder_name = funder.TidyOrgName(sid);
                    string? funder_lower = funder_name?.ToLower();

                    if (funder_lower is not null && sponsor_name is not null && 
                        funder_lower != sponsor_name.ToLower() && funder_lower.AppearsGenuineOrgName())
                    {
                        if (sponsor_name != "" && funder_name!.Contains(sponsor_name))
                        {
                            funder_name = funder_name.Replace(sponsor_name, "").Trim();
                        }

                        if (funder_name == "" || funder_lower.StartsWith("dr ") || funder_lower.StartsWith("dr. ")
                            || funder_lower.StartsWith("prof ") || funder_lower.StartsWith("prof. ")
                            || funder_lower.StartsWith("professor "))
                        {
                            // do nothing - unlikely to be a funder...
                        }
                        else
                        {
                            study_contributors.Add(new StudyContributor(sid, 58, "Study Funder", null, funder_name));
                        }
                    }
                }
            }
        }


        // Study leads and Contacts.

        string? study_lead = "";
        string? s_givenname = r.scientific_contact_givenname;
        string? s_familyname = r.scientific_contact_familyname;
        string? s_affiliation = r.scientific_contact_affiliation;
        if (!string.IsNullOrEmpty(s_givenname) || !string.IsNullOrEmpty(s_familyname))
        {
            string? full_name = (s_givenname + " " + s_familyname).Trim();
            full_name = full_name?.ReplaceApos();
            study_lead = full_name;  // for later comparison

            if (full_name.CheckPersonName())
            {
                full_name = full_name.TidyPersonName();
                if (!string.IsNullOrEmpty(full_name))
                {
                    s_affiliation = s_affiliation.TidyOrgName(sid);
                    string? affil_org = s_affiliation?.ExtractOrganisation(sid);
                    study_contributors.Add(new StudyContributor(sid, 51, "Study Lead", full_name, s_affiliation, affil_org));
                }
            }
        }

        // public contact
        string? p_givenname = r.public_contact_givenname;
        string? p_familyname = r.public_contact_familyname;
        string? p_affiliation = r.public_contact_affiliation;
        if (!string.IsNullOrEmpty(p_givenname) || !string.IsNullOrEmpty(p_familyname))
        {
            string? full_name = (p_givenname + " " + p_familyname).Trim();
            full_name = full_name?.ReplaceApos();
            if (full_name != study_lead)  // often duplicated
            {
                {
                    full_name = full_name.TidyPersonName();
                    if (full_name != "")
                    {
                        p_affiliation = p_affiliation.TidyOrgName(sid);
                        string? affil_org = p_affiliation?.ExtractOrganisation(sid);
                        study_contributors.Add(new StudyContributor(sid, 56, "Public Contact", full_name, p_affiliation, affil_org));
                    }
                }
            }
        }


        // Study features.

        var study_feats = r.study_features;
        if (study_feats?.Any() == true)
        {
            foreach(var f in study_feats)
            { 
                int? ftype_id = f.ftype_id;
                string? ftype = f.ftype;
                int? fvalue_id = f.fvalue_id;
                string? fvalue = f.fvalue;
                study_features.Add(new StudyFeature(sid, ftype_id, ftype, fvalue_id, fvalue));
            }
        }


        //study identifiers.

        var sids = r.secondary_ids;
        if (sids?.Any() == true)
        {
            foreach (var id in sids)
            {
                int? sec_id_source = id.sec_id_source;
                string? processed_id = id.processed_id;

                if (sec_id_source is not null)
                {
                    if (sec_id_source == 102000)
                    {
                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory Body ID", 102000, "Anvisa (Brazil)"));
                    }
                    else if (sec_id_source == 102001)
                    {
                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 12, "Ethics Review ID", 102001, "Comitê de Ética em Pesquisa (local) (Brazil)"));
                    }
                    else
                    {
                        source_name = wh.GetSourceName(sec_id_source);
                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", sec_id_source, source_name));
                    }
                }

                if (sec_id_source is null && sponsor_name is not null && processed_id is not null)
                {
                    string sponsor_name_lower = sponsor_name.ToLower();
                    if (sponsor_name_lower == "na" || sponsor_name_lower == "n/a" || sponsor_name_lower == "no"
                        || sponsor_name_lower == "none" || sponsor_name_lower == "not available"
                        || sponsor_name_lower == "no sponsor" || sponsor_name == "-" || sponsor_name == "--")
                    {
                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 1, "Type not provided", 12, "No organisation name provided in source data"));
                    }
                    else if (source_id == 100116)
                    {
                        study_identifiers.Add(wh.GetANZIdentifier(sid, processed_id, sponsor_is_org, sponsor_name));
                    }
                    else if (source_id == 100118)
                    {
                        StudyIdentifier? si = wh.GetChineseIdentifier(sid, processed_id, sponsor_is_org, sponsor_name);
                        if (si is not null)
                        {
                            study_identifiers.Add(si);
                        }
                    }
                    else if (source_id == 100127)
                    {
                        study_identifiers.Add(wh.GetJapaneseIdentifier(sid, processed_id, sponsor_is_org, sponsor_name));
                    }
                    else
                    {
                        if (sponsor_is_org == true)
                        {
                            study_identifiers.Add(new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name));
                        }
                        else
                        {
                            study_identifiers.Add(new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12, "No organisation name provided in source data"));
                        }
                    }
                }
            }
        }


        // Study conditions.

        var condList = r.condition_list;
        if (condList?.Any() == true)
        {
            foreach (var cn in condList)
            {
                string? con = cn.condition;
                if (!string.IsNullOrEmpty(con))
                {
                    char[] chars_to_trim = { ' ', '?', ':', '*', '/', '-', '_', '+', '=', '>', '<', '&' };
                    string contrim = con.Trim(chars_to_trim);
                    if (!string.IsNullOrEmpty(contrim) && contrim.ToLower() != "not applicable" && contrim.ToLower() != "&quot")
                    {
                        if (condition_is_new(contrim))
                        {
                            string? code = cn.code;
                            string? code_system = cn.code_system;

                            // Populate the conditions table rather than the topics table

                            if (code is null)
                            {
                                conditions.Add(new StudyCondition(sid, contrim, null, null));
                            }
                            else
                            {
                                if (code_system == "ICD 10")
                                {
                                    conditions.Add(new StudyCondition(sid, contrim, 12, cn.code));
                                }
                            }
                        }
                    }
                }
            }
        }


        bool condition_is_new(string candidate_condition)
        {
            foreach (StudyCondition k in conditions)
            {
                if (k is not null && k.original_value?.ToLower() == candidate_condition.ToLower())
                {
                    return false;
                }
            }
            return true;
        }


        // Create data object records.
        // Starting with Registry entry

        string? name_base = s.display_title;
        string? reg_prefix = wh.GetRegistryPrefix(source_id);
        if (reg_prefix is not null)
        {
            string object_title = reg_prefix + "registry web page";
            string object_display_title = name_base + " :: " + reg_prefix + "registry web page";
            string sd_oid = sid + " :: 13 :: " + object_title;

            int? pub_year = registration_date?.year;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, pub_year, 23, "Text", 13, "Trial Registry entry",
                source_id, source_name, 12, download_datetime));

            data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                "Study short name :: object type", true));

            string? remote_url = r.remote_url;
            if (remote_url is not null)
            {
                data_object_instances.Add(new ObjectInstance(sd_oid, source_id, source_name,
                                    remote_url, true, 35, "Web text"));
            }

            if (registration_date is not null)
            {
                data_object_dates.Add(new ObjectDate(sd_oid, 15, "Created", registration_date.year,
                            registration_date.month, registration_date.day, registration_date.date_string));
            }

            string? rec_date = r.record_date;
            if (!string.IsNullOrEmpty(rec_date))
            {
                SplitDate? record_date = rec_date.GetDatePartsFromISOString();
                data_object_dates.Add(new ObjectDate(sd_oid, 18, "Updated", record_date?.year,
                            record_date?.month, record_date?.day, record_date?.date_string));

            }
        }


        // There may be (rarely) a results link...usually but not always back to the 
        // source registry. Also rarely, a results_url_protocol - meaning unclear

        string? results_url_link = r.results_url_link;
        string? results_url_protocol = r.results_url_protocol;
        string? results_date_posted = r.results_date_posted;
        string? results_date_completed = r.results_date_completed;

        SplitDate? results_posted_date = results_date_posted?.GetDatePartsFromISOString();
        SplitDate? results_completed_date = results_date_completed?.GetDatePartsFromISOString();

        if (!string.IsNullOrEmpty(results_url_link))
        {
            // Exclude those on CTG - should be picked up there

            string results_link = results_url_link.ToLower();
            if (results_link.Contains("http") && !results_link.Contains("clinicaltrials.gov"))
            {
                string object_title = "Results summary";
                string object_display_title = name_base + " :: " + "Results summary";
                string sd_oid = sid + " :: 28 :: " + object_title;

                int? results_pub_year = results_posted_date?.year;

                // (in practice may not be in the registry)
                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_pub_year,
                                    23, "Text", 28, "Trial registry results summary",
                                    source_id, source_name, 12, download_datetime));

                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                    "Study short name :: object type", true));

                string url_link = Regex.Match(results_url_link, @"(http|https)://[\w-]+(\.[\w-]+)+([\w\.,@\?\^=%&:/~\+#-]*[\w@\?\^=%&/~\+#-])?").Value;
                data_object_instances.Add(new ObjectInstance(sd_oid, source_id, source_name,
                                    url_link, true, 35, "Web text"));

                if (results_posted_date != null)
                {
                    data_object_dates.Add(new ObjectDate(sd_oid, 12, "Available", results_posted_date.year,
                                results_posted_date.month, results_posted_date.day, results_posted_date.date_string));
                }
                if (results_completed_date != null)
                {
                    data_object_dates.Add(new ObjectDate(sd_oid, 15, "Created", results_completed_date.year,
                                results_completed_date.month, results_completed_date.day, results_completed_date.date_string));
                }
            }
        }


        if (!string.IsNullOrEmpty(results_url_protocol))
        {
            string prot_url = results_url_protocol.ToLower();
            if (prot_url.Contains("http") && !prot_url.Contains("clinicaltrials.gov"))
            {
                // presumed to be a download or a web reference
                string resource_type = "";
                int resource_type_id = 0;
                string url_link = "";
                int url_start = prot_url.IndexOf("http");

                if (results_url_protocol.Contains(".pdf"))
                {
                    resource_type = "PDF";
                    resource_type_id = 11;
                    int pdf_end = prot_url.IndexOf(".pdf") + 4;
                    url_link = results_url_protocol[url_start..pdf_end];

                }
                else if (prot_url.Contains(".doc"))
                {
                    resource_type = "Word doc";
                    resource_type_id = 16;
                    if (prot_url.Contains(".docx"))
                    {
                        int docx_end = prot_url.IndexOf(".docx") + 5;
                        url_link = results_url_protocol[url_start..docx_end];
                    }
                    else
                    {
                        int doc_end = prot_url.IndexOf(".doc") + 4;
                        url_link = results_url_protocol[url_start..doc_end];
                    }
                }
                else
                {
                    // most probably some sort of web reference
                    resource_type = "Web text";
                    resource_type_id = 35;
                    url_link = Regex.Match(results_url_protocol, @"(http|https)://[\w-]+(\.[\w-]+)+([\w\.,@\?\^=%&:/~\+#-]*[\w@\?\^=%&/~\+#-])?").Value;
                }


                int object_type_id = 0; string object_type = "";
                if (prot_url.Contains("study protocol"))
                {
                    object_type_id = 11;
                    object_type = "Study protocol";
                }
                else
                {
                    // most likely... but often difficult to tell
                    object_type_id = 79;
                    object_type = "CSR summary";
                }

                string object_display_title = name_base + " :: " + object_type;
                string object_title = object_type;
                string sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;
                int? results_pub_year = results_posted_date?.year;

                // almost certainly not in or managed by the registry

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_pub_year, 23, "Text", object_type_id, object_type,
                null, null, 11, download_datetime));

                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                    "Study short name :: object type", true));

                data_object_instances.Add(new ObjectInstance(sd_oid, null, null,
                                    url_link, true, resource_type_id, resource_type));
            }
        }


        var countryList = r.country_list;
        if (countryList?.Any() == true)
        {
            foreach (var country in countryList)
            {
                if (!string.IsNullOrEmpty(country))
                {
                    string country_name = country!;
                    country_name = country_name.Trim().ReplaceApos();

                    if (country_name.EndsWith(".") || country_name.EndsWith(",")
                        || country_name.EndsWith(")") || country_name.EndsWith("?")
                        || country_name.EndsWith("‘") || country_name.EndsWith("·")
                        || country_name.EndsWith("'"))
                    {
                        country_name = country_name[..^1];
                    }

                    country_name = country_name.Replace("(", " ").Replace(")", " ");
                    country_name = country_name.Replace("only ", "").Replace("Only in ", "");
                    country_name = country_name.Replace(" only", "").Replace(" Only", "");

                    var clower = country_name.ToLower();
                    if (clower.Length > 1 && clower != "na"
                        && clower != "n a" && clower != "other" && clower != "nothing"
                        && clower != "not applicable" && clower != "not provided"
                        && clower != "etc" && clower != "Under selecting")
                    {
                        if (clower != "none" && clower != "nnone"
                            && clower != "mone" && clower != "none."
                            && clower != "non" && clower != "noe"
                            && clower != "no country" && clower != "many"
                            && clower != "north" && clower != "south")
                        {
                            // The following can have misleading commas inside a name, that
                            // need to be removed before the string is split on the commas.

                            country_name = country_name.Replace("Palestine, State of", "State of Palestine");
                            country_name = country_name.Replace("Korea, Republic of", "South Korea");
                            country_name = country_name.Replace("Korea,Republic of", "South Korea");
                            country_name = country_name.Replace("Tanzania, United Republic Of", "Tanzania");
                            country_name = country_name.Replace("Korea, Democratic People’s Republic Of", "North Korea");
                            country_name = country_name.Replace("Korea, Democratic People’s Republic of", "North Korea");
                            country_name = country_name.Replace("Taiwan, Province Of China", "Taiwan");
                            country_name = country_name.Replace("Taiwan, Province of China", "Taiwan");
                            country_name = country_name.Replace("Taiwan, Taipei", "Taiwan");
                            country_name = country_name.Replace("Congo, The Democratic Republic Of The", "Democratic Republic of the Congo");
                            country_name = country_name.Replace("Japan,Asia except Japan", "Asia");
                            country_name = country_name.Replace("Japan, Japan", "Japan");

                            if (country_name.Contains(","))
                            {
                                string[] country_list = country_name.Split(",");
                                for (int i = 0; i < country_list.Length; i++)
                                {
                                    string ci = country_list[i].Trim();
                                    string cil = ci.ToLower();
                                    if (!cil.Contains("other") && !cil.Contains("countries")
                                        && cil != "islamic republic of"
                                        && cil != "republic of")
                                    {
                                        countries.Add(new StudyCountry(sid, ci));
                                    }
                                }
                            }
                            else
                            {
                                countries.Add(new StudyCountry(sid, country_name));
                            }
                        }
                    }
                }
            }
        }


        // Edit contributors - try to ensure properly categorised
             
        if (study_contributors.Count > 0)
        {
            foreach (StudyContributor sc in study_contributors)
            {
                if (sc.is_individual is not null)
                {
                    if ((bool)sc.is_individual)
                    {
                        // check if a group inserted as an individual.

                        string? fullname = sc.person_full_name?.ToLower();
                        if (fullname is not null)
                        {
                            if (wh.CheckIfOrganisation(fullname))
                            {
                                sc.organisation_name = sc.person_full_name.TidyOrgName(sid);
                                sc.person_full_name = null;
                                sc.is_individual = false;
                            }
                        }
                    }
                    else
                    {
                        // identify individuals down as organisations.

                        string? orgname = sc.organisation_name?.ToLower();
                        if (orgname is not null)
                        {
                            if (wh.CheckIfIndividual(orgname))
                            {
                                sc.person_full_name = sc.organisation_name.TidyPersonName();
                                sc.organisation_name = null;
                                sc.is_individual = true;
                            }
                        }
                    }
                }
            }
        }


        // add in the study properties
        s.identifiers = study_identifiers;
        s.titles = study_titles;
        s.features = study_features;
        s.contributors = study_contributors;
        s.countries = countries;
        s.conditions = conditions;
        s.iec = iec;

        s.data_objects = data_objects;
        s.object_titles = data_object_titles;
        s.object_dates = data_object_dates;
        s.object_instances = data_object_instances;

        return s;
    }
 
}
