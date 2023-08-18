using System.Globalization;
using System.Text.Json;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Isrctn;

public class IsrctnProcessor : IStudyProcessor
{

    public Study? ProcessData(string json_string, DateTime? download_datetime, ILoggingHelper _logging_helper)
    {
        ////////////////////////////////////////////////////////////////////////////////////////
        // Set up and deserialise string 
        ///////////////////////////////////////////////////////////////////////////////////////

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };


        ISCTRN_Record? r = JsonSerializer.Deserialize<ISCTRN_Record?>(json_string, json_options);
        if (r is null)
        {
            _logging_helper.LogError($"Unable to deserialise json file to Euctr_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }

        Study s = new();

        List<StudyIdentifier> identifiers = new();
        List<StudyTitle> titles = new();
        List<StudyOrganisation> organisations = new();
        List<StudyPerson> people = new();
        List<StudyReference> references = new();
        List<StudyTopic> topics = new();
        List<StudyFeature> features = new();
        List<StudyLocation> sites = new();
        List<StudyCountry> countries = new();
        List<StudyCondition> conditions = new();
        List<StudyIEC> iec = new();

        List<DataObject> data_objects = new();
        List<ObjectTitle> object_titles = new();
        List<ObjectDate> object_dates = new();
        List<ObjectInstance> object_instances = new();

        IsrctnHelpers ih = new();
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Basics - id, titles
        ///////////////////////////////////////////////////////////////////////////////////////
        
        string? sid = r.sd_sid;

        if (string.IsNullOrEmpty(sid))
        {
            _logging_helper.LogError($"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        // get basic study attributes

        string? study_name = r.title;
        if (!string.IsNullOrEmpty(study_name))
        {
            s.display_title = study_name.LineClean(); // = public title, default
            titles.Add(new StudyTitle(sid, s.display_title, 15, "Registry public title", true, "From ISRCTN"));
        }

        if (!string.IsNullOrEmpty(r.scientificTitle))
        {
            string sci_title = r.scientificTitle.LineClean()!;
            s.display_title ??= sci_title;
            if (sci_title != study_name)
            {
                titles.Add(new StudyTitle(sid, sci_title, 16, "Registry scientific title", s.display_title == sci_title,
                    "From ISRCTN"));
            }
        }

        if (!string.IsNullOrEmpty(r.acronym))
        {
            string acro = r.acronym;
            s.display_title ??= acro;
            if (!string.IsNullOrEmpty(r.acronym) && r.acronym.Length > 20)
            {
                // can a 'real' acronym be extracted from the long acronym?
                if (acro.Contains(':'))
                {
                    int colon_pos = acro.IndexOf(':');
                    acro = acro[..colon_pos];
                }
            }
            titles.Add(new StudyTitle(sid, acro, 14, "Acronym or Abbreviation", s.display_title == r.acronym, "From ISRCTN"));
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Study description
        ///////////////////////////////////////////////////////////////////////////////////////

        s.brief_description = r.plainEnglishSummary;  // the default. If not present use below...
        
        if (string.IsNullOrEmpty(s.brief_description) 
            || s.brief_description.ToLower().StartsWith("not provided"))
        {
            s.brief_description = "";
            string hypothesis = r.studyHypothesis.FullClean() ?? "";
            string pri_outcome = r.primaryOutcome.FullClean() ?? "";
            if (hypothesis.ToLower().StartsWith("not provided"))
            {
                hypothesis = "";
            }
            if (pri_outcome.ToLower().StartsWith("not provided"))
            {
                pri_outcome = "";
            }
            
            if (hypothesis != "")
            {
                if (!hypothesis.ToLower().StartsWith("hypothes") && !hypothesis.ToLower().StartsWith("study hyp"))
                {
                    hypothesis = "Study hypothesis: " + hypothesis;
                }
                s.brief_description = hypothesis;
            }
            if (pri_outcome != "")
            {
                if (!pri_outcome.ToLower().StartsWith("primary") && !pri_outcome.ToLower().StartsWith("outcome"))
                {
                    pri_outcome = "Primary outcome(s): " + pri_outcome;
                }
                s.brief_description += s.brief_description == "" ? pri_outcome : "\n" + pri_outcome;
            }
        }
        if (s.brief_description?.StartsWith("http") == true)  // can happen rarely
        {
            s.brief_description = "See " + s.brief_description;
        }
        if (string.IsNullOrEmpty(s.brief_description))  // a a fall back
        {
            s.brief_description =
                "No description or hypothesis / outcomes information provided at time of study registration";
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Study start date, type and status, registry page dates
        ///////////////////////////////////////////////////////////////////////////////////////

        string? ss_date = r.overallStartDate;
        if (!string.IsNullOrEmpty(ss_date))
        {
            SplitDate? study_start_date = ss_date[..10].GetDatePartsFromISOString();
            if (study_start_date is not null)
            {
                s.study_start_year = study_start_date.year;
                s.study_start_month = study_start_date.month;
            }
        }

        s.study_type = r.primaryStudyDesign;
        s.study_type_id = s.study_type.GetTypeId();

        // Study status from overall study status or more commonly from dates.
        // 'StatusOverride' field will only have a value if status is 'Suspended' or 'Stopped'.
        // More commonly compare dates with today to get current status.
        // Means periodic full import or a separate mechanism to update statuses against dates.
        // It appears that all 4 dates are always available.

        s.study_status = r.overallStatusOverride;
        if (s.study_status == "Stopped")
        {
            s.study_status = "Terminated";
        }
        else
        {
            string? se_date = r.overallEndDate;
            CultureInfo culture = CultureInfo.InvariantCulture;

            if (se_date is not null)
            {
                if (DateTime.TryParse(se_date, culture, DateTimeStyles.None, out DateTime se_date_dt))
                {
                    if (se_date_dt <= DateTime.Now)
                    {
                        s.study_status = "Completed";
                    }
                    else
                    {
                        // study is still ongoing - recruitment dates
                        // required for exact status.

                        string? rs_date = r.recruitmentStart;
                        string? re_date = r.recruitmentEnd;
                        if (DateTime.TryParse(rs_date, culture, DateTimeStyles.None, out DateTime rs_date_dt))
                        {
                            s.study_status = rs_date_dt > DateTime.Now 
                                ? "Not yet recruiting" : "Recruiting";
                        }

                        // But check if recruiting has now finished.

                        if (s.study_status == "Recruiting"
                            && DateTime.TryParse(re_date, culture, DateTimeStyles.None, out DateTime re_date_dt))
                        {
                            if (re_date_dt <= DateTime.Now)
                            {
                                s.study_status = "Active, not recruiting";
                            }
                        }
                    }
                }
            }
        }
        s.study_status_id = s.study_status.GetStatusId();

        // study registry entry dates.

        SplitDate? reg_date = null;
        SplitDate? last_edit = null;

        string? r_date = r.dateIdAssigned;
        if (!string.IsNullOrEmpty(r_date))
        {
            reg_date = r_date[..10].GetDatePartsFromISOString();
        }
        string? d_edited = r.lastUpdated;
        if (!string.IsNullOrEmpty(d_edited))
        {
            last_edit = d_edited[..10].GetDatePartsFromISOString();
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Study sponsors and funders
        ///////////////////////////////////////////////////////////////////////////////////////

        var sponsors = r.sponsors;
        string? sponsor_name = null;    // For later use
        if (sponsors?.Any() is true)
        {
            foreach (var stSponsor in sponsors)
            {
                string? org = stSponsor.organisation;
                if (org.IsNotPlaceHolder() && org.AppearsGenuineOrgName())
                {
                    string? org_name = org.TidyOrgName(sid).StandardisePharmaName();
                    organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", null, org_name));
                }
            }
            if (organisations.Any())
            {
                sponsor_name = organisations[0].organisation_name;
            }
        }

        var funders = r.funders;
        if (funders?.Any() is true)
        {
            foreach (var funder in funders)
            {
                string? funder_name = funder.name;
                if (funder_name.IsNotPlaceHolder() && funder_name.AppearsGenuineOrgName())
                {
                    // Situation where the same organisation is the sponsor and funder
                    // now resolved in the Coding module, but a funder can be repeated.
                    // N.B. All organisations assumed to be funders (type id = 58)
                    
                    funder_name = funder_name.TidyOrgName(sid).StandardisePharmaName();
                    if (funder_name!.IsNotInOrgsAsRoleAlready(58, organisations))
                    {
                        organisations.Add(new StudyOrganisation(sid, 58, "Study Funder", null, funder_name));
                    }
                }
            }
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Study contacts / contributors
        ///////////////////////////////////////////////////////////////////////////////////////

        var contacts = r.contacts;
        if (contacts?.Any() is true)
        {
            foreach (var contact in contacts)
            {
                string? cType = contact.contactType;
                string givenName = contact.forename.TidyPersonName() ?? "";
                string familyName = contact.surname.TidyPersonName() ?? "";

                string? affil =  contact.address?.TidyOrgName(sid).StandardisePharmaName();
                string? affil_organisation = affil?.ExtractOrganisation(sid);
                
                string? orcid = contact.orcid.TidyORCIDId();
                string full_name = (givenName + " " + familyName).Trim();

                int contrib_type_id = 0;
                string? contrib_type = cType;
                if (cType is "Scientific" or "Principal Investigator")
                {
                    contrib_type_id = 51;
                    contrib_type = "Study Lead";
                }
                else if (cType == "Public")
                {
                    contrib_type_id = 56;
                    contrib_type = "Public contact";
                }

                if (contrib_type_id is 51 or 56) 
                {
                    if (full_name != "")
                    {
                        people.Add(new StudyPerson(sid, contrib_type_id, contrib_type, givenName,
                            familyName, full_name, orcid, affil, null, affil_organisation));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(affil))
                        {
                            organisations.Add(new StudyOrganisation(sid, contrib_type_id, contrib_type,
                                null, affil_organisation));
                        }
                    }
                }
            }
        }

        // Try to ensure contributors are properly categorised.
        // Check if a group has been inserted as an individual,
        // or an individual has been inserted as a group.
        
        // Edit contributors - try to ensure properly categorised
        // check if a group inserted as an individual, and then
        // check if an individual added as a group.
        
        List<StudyPerson> people2 = new();
        if (people.Count > 0)
        {
            bool add = true;
            foreach (StudyPerson p in people)
            {
                string? full_name = p.person_full_name?.ToLower();
                if (full_name is not null && !full_name.AppearsGenuinePersonName())
                {
                    string? organisation_name = p.person_full_name.TidyOrgName(sid);
                    if (organisation_name is not null)
                    {
                        organisations.Add(new StudyOrganisation(sid, p.contrib_type_id, p.contrib_type,
                            null, organisation_name));
                        add = false;
                    }
                }
                if (add)
                {
                    people2.Add(p);
                }
            }
        }
        
        List<StudyOrganisation> orgs2 = new();
        if (organisations.Count > 0)
        {
            foreach (StudyOrganisation g in organisations)
            {
                bool add = true;
                string? org_name = g.organisation_name?.ToLower();
                if (org_name is not null && !org_name.AppearsGenuineOrgName())
                {
                    string? person_full_name = g.organisation_name.TidyPersonName();
                    if (person_full_name is not null)
                    {
                        people2.Add(new StudyPerson(sid, g.contrib_type_id, g.contrib_type, person_full_name,
                            null, null, g.organisation_name));
                        add = false;
                    }
                }
                if (add)
                {
                    orgs2.Add(g);
                }
            }
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Study locations
        ///////////////////////////////////////////////////////////////////////////////////////

        // Do these before identifiers as 'location in UK only' used to help identify some identifier types.
        // N.B. Some countries have already been renamed and checked for duplication
        // as part of the download process

        var country_list = r.recruitmentCountries;
        if (country_list?.Any() is true)
        {
            foreach (string c in country_list)
            {
                countries.Add(new StudyCountry(sid, c));
            }
        }
        
        int in_uk_only = 0;
        if (country_list is ["United Kingdom"])
        {
            in_uk_only = 2;
        }
        else if (country_list?.Count > 1)
        {
            foreach (string c in country_list)
            {
                if (c == "United Kingdom")
                {
                    in_uk_only = 1;
                    break;
                } 
            }
        }

        var locations = r.centres;
        if (locations?.Any() is true)
        {
            foreach (var loc in locations)
            {
                sites.Add(new StudyLocation(sid, loc.name, loc.city, loc.country, null, null));
            }
        }
        
        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Study identifiers
        ///////////////////////////////////////////////////////////////////////////////////////

        identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100126, 
                                            "ISRCTN", reg_date?.date_string, null));

        var idents = r.identifiers;
        if (idents?.Any() is true)
        {
            foreach (var ident in idents)
            {
                string? iType = ident.identifier_type?.Trim();
                string? value = ident.identifier_value;
                if (!string.IsNullOrEmpty(value) && value.IsNotPlaceHolder())
                {
                    if (iType != "To be determined")
                    {
                        identifiers.Add(new StudyIdentifier(sid, ident.identifier_value, ident.identifier_type_id,
                                iType, ident.identifier_org_id, ident.identifier_org, null, null));
                    }
                    else
                    {
                        // Classed as a 'serial protocol number':  already split if included a ';' or ','
                        // need to try and determine what it is.
                        
                        if (!string.IsNullOrEmpty(ident.identifier_value))
                        {
                            List<IsrctnIdentifierDetails> idds =
                                ih.GetISRCTNIdentifierProps(ident.identifier_value, sponsor_name, in_uk_only);
                            if (idds.Any())
                            {
                                foreach (IsrctnIdentifierDetails idd in idds)
                                {
                                    if (idd.id_value.IsNewToList(identifiers))
                                    {
                                        identifiers.Add(new StudyIdentifier(sid, idd.id_value, idd.id_type_id,
                                            idd.id_type, idd.id_org_id, idd.id_org, null, null));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        

        ////////////////////////////////////////////////////////////////////////////////////////
        // Design info and features
        ///////////////////////////////////////////////////////////////////////////////////////

        string? phase = r.phase;
        if (!string.IsNullOrEmpty(phase) && s.study_type_id == 11)
        {
            Tuple<int, string, int, string> new_feature = phase switch
            {
                "Not Applicable" => new Tuple<int, string, int, string>(20, "Phase", 100, "Not applicable"),
                "Phase I" => new Tuple<int, string, int, string>(20, "Phase", 110, "Phase 1"),
                "Phase I/II" => new Tuple<int, string, int, string>(20, "Phase", 115, "Phase 1/Phase 2"),
                "Phase II" => new Tuple<int, string, int, string>(20, "Phase", 120, "Phase 2"),
                "Phase II/III" => new Tuple<int, string, int, string>(20, "Phase", 125, "Phase 2/Phase 3"),
                "Phase III" => new Tuple<int, string, int, string>(20, "Phase", 130, "Phase 3"),
                "Phase III/IV" => new Tuple<int, string, int, string>(20, "Phase", 130, "Phase 3"),
                "Phase IV" => new Tuple<int, string, int, string>(20, "Phase", 135, "Phase 4"),
                "Not Specified" => new Tuple<int, string, int, string>(20, "Phase", 140, "Not provided"),
                _ => new Tuple<int, string, int, string>(20, "Phase", 140, "Not provided"),
            };

            if (new_feature.Item4 != "Not provided")
            {
                features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                    new_feature.Item3, new_feature.Item4));
            }
        }

        // Other features can be found in secondary design and / or study design fields. Concatenate these
        // before searching them. Interventional study features considered first, then observational features

        string secondary_design = r.secondaryStudyDesign ?? "";
        string study_design = r.studyDesign ?? "";
        string design = (secondary_design + " " + study_design).ToLower();

        if (design != "")
        {
            Tuple<int, string, int, string> new_feature;

            if (s.study_type_id == 11)
            {
                string st_des = design.Replace("randomized", "randomised")
                         .Replace("non randomised", "non-randomised");

                new_feature = st_des switch
                {
                    _ when st_des.Contains("non-randomised") => new Tuple<int, string, int, string>(22, "allocation type", 210, "Nonrandomised"),
                    _ when st_des.Contains("randomised") => new Tuple<int, string, int, string>(22, "allocation type", 205, "Randomised"),
                    _ => new Tuple<int, string, int, string>(22, "allocation type", 215, "Not provided")
                };

                if (new_feature.Item4 != "Not provided")
                {
                    features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                        new_feature.Item3, new_feature.Item4));
                }

                st_des = design.Replace("cross over", "cross-over")
                         .Replace("crossover", "cross-over");

                new_feature = st_des switch
                {
                    _ when st_des.Contains("parallel") => new Tuple<int, string, int, string>(23, "Intervention model", 305, "Parallel assignment"),
                    _ when st_des.Contains("cross-over") => new Tuple<int, string, int, string>(23, "Intervention model", 310, "Crossover assignment"),
                    _ => new Tuple<int, string, int, string>(0, "", 0, "")
                };

                if (new_feature.Item1 != 0)
                {
                    features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                                       new_feature.Item3, new_feature.Item4));
                }

                st_des = design.Replace("open label", "open-label")
                            .Replace(" blind", "-blind");

                new_feature = st_des switch
                {
                    _ when st_des.Contains("open-label") => new Tuple<int, string, int, string>(24, "Masking", 500, "None (Open Label)"),
                    _ when st_des.Contains("single-blind") => new Tuple<int, string, int, string>(24, "Masking", 505, "Single"),
                    _ when st_des.Contains("double-blind") => new Tuple<int, string, int, string>(24, "Masking", 510, "Double"),
                    _ when st_des.Contains("triple-blind") => new Tuple<int, string, int, string>(24, "Masking", 515, "Triple"),
                    _ when st_des.Contains("quadruple-blind") => new Tuple<int, string, int, string>(24, "Masking", 520, "Quadruple"),
                    _ => new Tuple<int, string, int, string>(24, "Masking", 525, "Not provided")
                };

                if (new_feature.Item4 != "Not provided")
                {
                    features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                        new_feature.Item3, new_feature.Item4));
                }
            }

            if (s.study_type_id == 12)
            {
                string st_des = design.Replace("case ", "case-");

                new_feature = st_des switch
                {
                    _ when st_des.Contains("cohort") => new Tuple<int, string, int, string>(30, "Observational model", 600, "Cohort"),
                    _ when st_des.Contains("case-control") => new Tuple<int, string, int, string>(30, "Observational model", 605, "Case-Control"),
                    _ when st_des.Contains("case-series") => new Tuple<int, string, int, string>(30, "Observational model", 610, "Case-only"),
                    _ when st_des.Contains("case-crossover") => new Tuple<int, string, int, string>(30, "Observational model", 615, "Case-crossover"),
                    _ when st_des.Contains("ecological") => new Tuple<int, string, int, string>(30, "Observational model", 620, "Ecologic or community study"),
                    _ => new Tuple<int, string, int, string>(0, "", 0, "")
                };

                if (new_feature.Item1 != 0)
                {
                    features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                                       new_feature.Item3, new_feature.Item4));
                }

                new_feature = st_des switch
                {
                    _ when st_des.Contains("retrospective") => new Tuple<int, string, int, string>(31, "Time perspective", 700, "Retrospective"),
                    _ when st_des.Contains("prospective") => new Tuple<int, string, int, string>(31, "Time perspective", 705, "Prospective"),
                    _ when st_des.Contains("cross section") => new Tuple<int, string, int, string>(31, "Time perspective", 710, "Cross-sectional"),
                    _ when st_des.Contains("longitudinal") => new Tuple<int, string, int, string>(31, "Time perspective", 730, "Longitudinal"),
                    _ => new Tuple<int, string, int, string>(0, "", 0, "")
                };

                if (new_feature.Item1 != 0)
                {
                    features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                                       new_feature.Item3, new_feature.Item4));
                }
            }
        }

        // Trial type provides primary purpose.

        string? trial_type = r.trialType;
        if (trial_type is not null)
        {
            Tuple<int, string, int, string> new_feature = phase switch
            {
                "Treatment" => new Tuple<int, string, int, string>(21, "primary purpose", 400, "Treatment"),
                "Prevention" => new Tuple<int, string, int, string>(21, "primary purpose", 405, "Prevention"),
                "Diagnostic" => new Tuple<int, string, int, string>(21, "primary purpose", 410, "Diagnostic"),
                "Screening" => new Tuple<int, string, int, string>(21, "primary purpose", 420, "Screening"),
                "Quality of life" => new Tuple<int, string, int, string>(21, "primary purpose", 440, "Other"),
                "Other" => new Tuple<int, string, int, string>(21, "primary purpose", 440, "Other"),
                "Not Specified" => new Tuple<int, string, int, string>(21, "primary purpose", 445, "Not provided"),
                _ => new Tuple<int, string, int, string>(21, "primary purpose", 445, "Not provided"),
            };

            if (new_feature.Item4 != "Not provided")
            {
                features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                    new_feature.Item3, new_feature.Item4));
            }

        }


        ////////////////////////////////////////////////////////////////////////////////////////
        // Design topics and conditions
        ///////////////////////////////////////////////////////////////////////////////////////

        List<string> topic_names = new();

        string? drugNames = r.drugNames;
        if (!string.IsNullOrEmpty(drugNames) && drugNames != "N/A"
              && !drugNames.ToLower().StartsWith("the sponsor has confirmed")
              && !drugNames.ToLower().StartsWith("the health research authority (hra) has approved"))
        {
            drugNames = drugNames.LineClean();
            if (drugNames is not null)
            {
                if (drugNames.Contains("1.") && drugNames.Contains("2."))
                {
                    // Numbered list (almost certainly) - split and add list

                    List<string> numbered_strings = drugNames.GetNumberedStrings(".", 8);
                    topic_names.AddRange(numbered_strings);
                }
                else if ((r.interventionType is "Drug" or "Supplement") && drugNames.Contains(','))
                {
                    // if there are commas split on the commas (does not work well for devices).

                    List<string>? split_drug_names = drugNames.SplitStringWithMinWordSize(',', 4);
                    if (split_drug_names is not null)
                    {
                        topic_names.AddRange(split_drug_names);
                    }
                }
                else
                {
                    topic_names.Add(drugNames);
                }

                string topic_type = r.interventionType == "Device" ? "Device" : "Chemical / agent";
                int topic_type_id = r.interventionType == "Device" ? 21 : 12;
                foreach (string tn in topic_names)
                {
                    topics.Add(new StudyTopic(sid, topic_type_id, topic_type, tn.Trim()));
                }

                topics = topics.RemoveNonInformativeTopics();
            }
        }

        string? listed_condition = r.conditionDescription;
        if (string.IsNullOrEmpty(listed_condition))
        {
            listed_condition = r.diseaseClass1;  // use this only if the first field empty
        }
       
        if (!string.IsNullOrEmpty(listed_condition))
        {
            // Can be very general - high level classifications, but can be a delimited list.
            List<string> conds = new();
            
            if (listed_condition.Contains(','))
            {
                string[] cons = listed_condition.Split(',');
                foreach (var c in cons)
                {
                    conds.Add(c);
                }
            }
            else if (listed_condition.Contains(';'))
            {
                string[] cons = listed_condition.Split(';');
                foreach (var c in cons)
                {
                    conds.Add(c);
                }
            }
            else if (listed_condition.Contains("1.") && listed_condition.Contains("2."))
            {
                // Numbered list (almost certainly) - split and add list

                List<string> cons = listed_condition.GetNumberedStrings(".", 8);
                conds.AddRange(cons);
            }
            else
            {
                conditions.Add(new StudyCondition(sid, listed_condition, null, null, null));
            }

            foreach (string cond1 in conds)
            {
                // Remove spurious text
                
                string cond = cond1;
                if (!cond.StartsWith("Topic") 
                    && !cond.StartsWith("Primary Care Research Network")
                    && !cond.StartsWith("Healthy")
                    && !cond.ToLower().StartsWith("not applicable"))
                {
                    cond = cond.Replace("Generic Health Relevance and Cross Cutting Themes", "");
                    cond = cond.Replace("Generic Health Relevance", "");
                    cond = cond.Replace("Primary sub-specialty:", "");
                    cond = cond.Replace("UKCRC code/ Disease:", "");       
                    cond = cond.Replace("(all Subtopics)", "");                    
                    cond = cond.Replace("All Diseases", "");                   
                    cond = cond.Replace("Subtopic: ", "");
                    cond = cond.Replace("Disease:", "");
                    cond = cond.Replace("Not assigned", "");
                    cond = cond.Replace("Not Assigned", "");
                    cond = cond.Replace("Specialty:", "");
                    cond = cond.Replace("Signs and Symptoms:", "");
                    cond = cond.Replace(";", "");
                    if (cond.StartsWith(" and"))
                    {
                        cond = cond[4..].Trim();
                    }
                    cond = cond.Trim();

                    if (cond != "")
                    {
                        conditions.Add(new StudyCondition(sid, cond, null, null, null));
                    }
                }
            }

            conditions = conditions.RemoveNonInformativeConditions();
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Eligibility
        ///////////////////////////////////////////////////////////////////////////////////////

        string? final_enrolment = r.totalFinalEnrolment;
        string? target_enrolment = r.targetEnrolment?.ToString();

        if (!string.IsNullOrEmpty(target_enrolment) && target_enrolment != "Not provided at time of registration"
             && target_enrolment != "0")
        {
            s.study_enrolment = target_enrolment + " (target)";
        }

        if (!string.IsNullOrEmpty(final_enrolment) && final_enrolment != "Not provided at time of registration"
                                                   && final_enrolment != "0")
        {
            if (string.IsNullOrEmpty(s.study_enrolment))
            {
                s.study_enrolment = final_enrolment + " (final)";
            }
            else
            {
                s.study_enrolment += ", " + final_enrolment + " (final)";
            }
        }

        string? gender = r.gender; 
        if (!string.IsNullOrEmpty(gender)) 
        {
            s.study_gender_elig = gender;
            if (s.study_gender_elig == "Both")
            {
                s.study_gender_elig = "All";
            }
            if (s.study_gender_elig == "Not Specified")
            {
                s.study_gender_elig = "Not provided";
            }
            s.study_gender_elig_id = s.study_gender_elig.GetGenderEligId();
        }

        string? age_group = r.ageRange;
        if (!string.IsNullOrEmpty(age_group) && age_group != "Mixed"
            && age_group != "Not Specified" && age_group != "All")
        {
            Tuple<int?, string?, int?, string?> age_params = age_group switch
            {
                "Neonate" => new Tuple<int?, string?, int?, string?>(null, null, 28, "Days"),
                "Child" => new Tuple<int?, string?, int?, string?>(29, "Days", 17, "Years"),
                "Adult" => new Tuple<int?, string?, int?, string?>(18, "Years", 65, "Years"),
                "Senior" => new Tuple<int?, string?, int?, string?>(66, "Years", null, null),
                _ => new Tuple<int?, string?, int?, string?>(null, null, null, null)
            };

            if (age_params.Item1 is not (null and null))
            {
                s.min_age = age_params.Item1;
                s.min_age_units = age_params.Item2;
                s.min_age_units_id = s.min_age_units.GetTimeUnitsId();
                s.max_age = age_params.Item3;
                s.max_age_units = age_params.Item4;
                s.max_age_units_id = s.max_age_units.GetTimeUnitsId();
            }
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Inclusion / Exclusion criteria
        ///////////////////////////////////////////////////////////////////////////////////////

        string? ic = r.inclusion;
        string? ec = r.exclusion;
        int num_inc_criteria = 0;
        int study_iec_type = 0;
        
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
       
        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Design sharing
        ///////////////////////////////////////////////////////////////////////////////////////
        
        // At the moment these seem to be a single string summarising the management of IPD.

        string? ipd_ss = r.ipdSharingStatement;
        if (!string.IsNullOrEmpty(ipd_ss) && ipd_ss != "Not provided at time of registration")
        {
            s.data_sharing_statement = ipd_ss.FullClean();
        }
        else
        {
            s.data_sharing_statement = "";
        }
        
        var data_policies = r.dataPolicies;
        if (data_policies?.Any() is true)
        {
            if (data_policies.Count == 1 && data_policies[0] != "Not provided at time of registration")
            {
                s.data_sharing_statement += "\nIPD policy summary: " + data_policies[0];
            }
            else
            {
                int n = 0;                
                foreach (string policy in data_policies)
                {
                    if (policy != "Not provided at time of registration")
                    {
                        n++;
                        if (n == 1)
                        {
                            s.data_sharing_statement += "\nIPD policy summary: \n1) " + policy;
                        }
                        else
                        {
                            s.data_sharing_statement += $"\n{n}) " + policy;
                        }
                    }
                }
            }
        }

        if (s.data_sharing_statement == "")
        {
            s.data_sharing_statement = null;
        }
        
        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Registry entry data object
        ///////////////////////////////////////////////////////////////////////////////////////

        int? pub_year = null;
        if (reg_date is not null)
        {
            pub_year = reg_date.year;
        }
        string object_title = "ISRCTN registry entry";
        string object_display_title = s.display_title + " :: ISRCTN registry entry";

        // create Id for the data object.

        string sd_oid = sid + " :: 13 :: " + object_title;

        DataObject dobj = new (sd_oid, sid, object_title, object_display_title, pub_year,
                23, "Text", 13, "Trial Registry entry", 100126, "ISRCTN", 12, download_datetime)
        {
            doi = r.doi,
            doi_status_id = 1
        };

        data_objects.Add(dobj);

        // Data object title is the display title...

        object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                                    22, "Study short name :: object type", true));
        if (last_edit is not null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                                last_edit.year, last_edit.month, last_edit.day, last_edit.date_string));
        }

        if (reg_date is not null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 15, "Created",
                                reg_date.year, reg_date.month, reg_date.day, reg_date.date_string));
        }

        // Instance url can be derived from the ISRCTN number.

        object_instances.Add(new ObjectInstance(sd_oid, 100126, "ISRCTN",
                    "https://www.isrctn.com/" + sid, true, 39, "Web text with XML or JSON via API"));


        ////////////////////////////////////////////////////////////////////////////////////////
        // Patient Information Sheets data object
        ///////////////////////////////////////////////////////////////////////////////////////
        
        // N.B. PIS details now seem to have been largely transferred to the 'Additional files' section.

        string? PIS_details = r.patientInfoSheet;
        if (PIS_details is not null && !PIS_details.StartsWith("Not available") 
             && !PIS_details.StartsWith("Not applicable") && PIS_details != "See additional files")
        {
            if (PIS_details.Contains("http"))
            {
                // PIS note includes a url to a web address or web based file
                int ref_start = PIS_details.IndexOf("http", StringComparison.Ordinal);
                string href = PIS_details[ref_start..];

                // check if link does not provide a 404 - to be re-implemented
                if (true) //await HtmlHelpers.CheckURLAsync(href))
                {
                    int res_type_id = 35;
                    string res_type = "Web text";     // defaults
                    if (href.ToLower().Contains(".pdf"))
                    {
                        res_type_id = 11;
                        res_type = "PDF";
                    }
                    else if (href.ToLower().Contains(".docx") || href.ToLower().Contains(".doc"))
                    {
                        res_type_id = 16;
                        res_type = "Word doc";
                    }

                    object_title = "Patient information sheet";
                    object_display_title = s.display_title + " :: patient information sheet";
                    sd_oid = sid + " :: 19 :: " + object_title;

                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, s.study_start_year,
                       23, "Text", 19, "Patient information sheets", null, sponsor_name, 12, download_datetime));
                    object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                                        22, "Study short name :: object type", true));
                    object_instances.Add(new ObjectInstance(sd_oid, null, "", href, true, res_type_id, res_type));
                }
            }
        }
        
        
        ////////////////////////////////////////////////////////////////////////////////////////
        // Possible web site data object
        ///////////////////////////////////////////////////////////////////////////////////////

        string? trial_website = r.trialWebsite;
        if (!string.IsNullOrEmpty(trial_website) && trial_website.Contains("http"))
        {
            object_title = "Study web site";
            sd_oid = sid + " :: 134 :: " + object_title;
            object_display_title = s.display_title + " :: Study web site";

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, s.study_start_year,
                    23, "Text", 134, "Website", null, sponsor_name, 12, download_datetime));
            
            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                                    22, "Study short name :: object type", true));
            
            object_instances.Add(new ObjectInstance(sd_oid, null, sponsor_name, trial_website, true, 35, "Web text"));
        }


        ////////////////////////////////////////////////////////////////////////////////////////
        // Additional files and external links data objects
        ///////////////////////////////////////////////////////////////////////////////////////

        // Output list appears to be composed of both external links to published papers and local files
        // of unpublished supplementary material. External links may be published papers - almost always
        // referred to by pubmed ids, or links to web sites or unpublished papers with different types of
        // information on them. Local filers are often PIS, but may be result summaries and other objects.

        var outputs = r.outputs;
        if(outputs?.Any() is true)
        {
            foreach (StudyOutput op in outputs)
            {
                string? output_type = op.outputType;
                if (!string.IsNullOrEmpty(output_type))
                {
                    string output_lower = output_type.ToLower();
                    Tuple<int, string, int, string> object_details = output_lower switch
                    {
                        "resultsarticle" => new Tuple<int, string, int, string>(23, "Text", 202, "Journal article - results"),
                        "interimresults" => new Tuple<int, string, int, string>(23, "Text", 203, "Journal article - interim results"),
                        "protocolarticle" => new Tuple<int, string, int, string>(23, "Text", 201, "Journal article - protocol"),
                        "funderreport" => new Tuple<int, string, int, string>(23, "Text", 204, "Journal article - review"),
                        "preprint" or "protocolpreprint" or 
                        "preprintother" => new Tuple<int, string, int, string>(23, "Text", 210, "Preprint article"),
                        "otherpublications" => new Tuple<int, string, int, string>(23, "Text", 12, "Journal article - unspecified"),
                        "pis" or
                        "participant information sheet" or
                        "patient information sheet" => new Tuple<int, string, int, string>(23, "Text", 19, "Patient information sheets"),
                        "basicresults" or "thesis" or
                        "poster" or "otherunpublishedresults" or
                        "bookresults" or "abstract" => new Tuple<int, string, int, string>(23, "Text", 79, "Results or CSR summary"),
                        "protocolfile" or
                        "protocolother" => new Tuple<int, string, int, string>(23, "Text", 11, "Study Protocol"),
                        "hrasummary" => new Tuple<int, string, int, string>(23, "Text", 38, "Study overview"),
                        "dataset" => new Tuple<int, string, int, string>(14, "Dataset", 80, "Individual participant data"),
                        "plainenglishresults" => new Tuple<int, string, int, string>(23, "Text", 88, "Summary of results for public"),
                        "sap" => new Tuple<int, string, int, string>(23, "Text", 22, "Statistical analysis plan"),
                        _ when output_lower.Contains("analysis") => new Tuple<int, string, int, string>(23, "Text", 22, "Statistical analysis plan"),
                        _ when output_lower.Contains("consent") => new Tuple<int, string, int, string>(23, "Text", 18, "Informed consent forms"),
                        "otherfiles" => new Tuple<int, string, int, string>(23, "Text", 37, "Other text based object"),
                        "book" => new Tuple<int, string, int, string>(23, "Text", 101, "Book"),
                        "trialwebsite" => new Tuple<int, string, int, string>(23, "Text", 134, "Website"),
                        _ => new Tuple<int, string, int, string>(0, "Text", 0, output_lower),
                    };

                    if (object_details.Item1 == 0)
                    {
                        // may be a full title in the type field rather than a type name

                        if (output_lower.Contains("results") || output_lower.Contains("report") 
                                                             || output_lower.Contains("data analysis"))
                        {
                            object_details = new Tuple<int, string, int, string>(23, "Text", 79, "Results or CSR summary");
                        }
                        else if (output_lower.Contains("preprint"))
                        {
                            object_details = new Tuple<int, string, int, string>(23, "Text", 210, "Preprint article");
                        }
                        else if (output_lower.Contains("diagram"))
                        {
                            object_details = new Tuple<int, string, int, string>(23, "Text",  37, "Other text based object");
                        }
                    }

                    int object_class_id = object_details.Item1;
                    string object_class = object_details.Item2;
                    int object_type_id = object_details.Item3;
                    string object_type = object_details.Item4;

                    string? artefact_type = op.artefactType;
                    string? external_url = op.externalLinkURL;
                    string? local_url = op.localFileURL;
                    if (op.version == "")
                    {
                        op.version = null;  // to clarify!
                    }
                    
                    if (artefact_type == "ExternalLink" && !string.IsNullOrEmpty(external_url))
                    {
                        string citation = external_url; // for storage 'as is' for later inspection
                        if (external_url.ToLower().Contains("pubmed"))
                        {
                            // Some sort of reference to a published article
                            // Tidy up url and try and get a pmid

                            int pmid = 0;
                            bool pmid_found = false;
                            char[] end_charsToTrim = { ';', '.', '/', '?' };
                            external_url = external_url.TrimEnd(end_charsToTrim);

                            if (external_url.Contains("list_uids="))
                            {
                                string poss_pmid = external_url[(external_url
                                                   .IndexOf("list_uids=", StringComparison.Ordinal) + 10)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else if (external_url.Contains("termtosearch="))
                            {
                                string poss_pmid = external_url[(external_url
                                                  .IndexOf("termtosearch=", StringComparison.Ordinal) + 13)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else if (external_url.Contains("term="))
                            {
                                string poss_pmid = external_url[(external_url
                                                  .IndexOf("term=", StringComparison.Ordinal) + 5)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else
                            {
                                // 'just' /<pubmed_id> at the end ...
                                int pubmed_pos = external_url.LastIndexOf("/", StringComparison.Ordinal);
                                if (pubmed_pos != -1)
                                {
                                    string poss_pmid = external_url[(pubmed_pos + 1)..];
                                    if (int.TryParse(poss_pmid, out pmid))
                                    {
                                        pmid_found = true;
                                    }
                                }
                            }

                            string? pmid_string = null;
                            if (pmid_found && pmid > 0)
                            {
                                pmid_string = pmid.ToString();
                            }

                            references.Add(new StudyReference(sid, pmid_string, citation, null, object_type_id,
                                object_type, output_type));
                        }
                        else
                        {
                            // Almost certainly an object / resource within a web page or as an
                            // unpublished web document. Create a data object record of this type,
                            // but ignore results records likely to be found in other sources.

                            if (!external_url.Contains("clinicaltrialsregister.eu") &&
                                !external_url.Contains("clinicaltrials.gov"))
                            {
                                // Use object type as name and add any version
                                // to name if one present. Then construct 
                                // display title and the sd_oid
                                
                                string object_name = object_type;
                                if (!string.IsNullOrEmpty(op.version))
                                {
                                    object_name += " " + op.version;
                                }

                                object_display_title = s.display_title + " :: " + object_name;
                                sd_oid = sid + " :: " + object_type_id + " :: " + object_type;

                                // Check the sd_oid has not been created before, and add
                                // a differentiating suffix if that is the case.
                                
                                int next_num = checkOID(sd_oid, data_objects);
                                if (next_num > 0)
                                {
                                    sd_oid += "_" + next_num;
                                    object_display_title += "_" + next_num;
                                }
                                
                                // Get the date if possible, and get the doi if present.

                                SplitDate? dt_created = null;
                                if (!string.IsNullOrEmpty(op.dateCreated))
                                {
                                    dt_created = op.dateCreated?[..10].GetDatePartsFromISOString();
                                }

                                DataObject d_obj = new(sd_oid, sid, object_type, object_display_title,
                                    dt_created?.year, object_class_id, object_class, object_type_id, 
                                    object_type, 100126, "ISRCTN", 11, download_datetime)
                                {
                                    version = op.version
                                };

                                string doi = "";
                                if (external_url.Contains("doi"))
                                {
                                    if (external_url.IndexOf("doi.org/", StringComparison.Ordinal) != -1)
                                    {
                                        doi = external_url[(external_url.IndexOf("doi.org/", StringComparison.Ordinal) + 8)..];
                                    }
                                    else if (external_url.IndexOf("/10.", StringComparison.Ordinal) != -1)
                                    {
                                        doi = external_url[(external_url.IndexOf("/10.", StringComparison.Ordinal) + 1)..];
                                    }
                                }

                                if (doi != "")
                                {
                                    d_obj.doi = doi;
                                    d_obj.doi_status_id = 1;
                                }
          
                                data_objects.Add(d_obj);

                                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                    22, "Study short name :: object type", true));

                                Tuple<int, string> system_details = external_url switch
                                { 
                                    _ when external_url.Contains("articles/PMC") => new Tuple<int, string>(100133, "National Library of Medicine"),
                                    _ when external_url.Contains("hra.nhs.uk") => new Tuple<int, string>(109372, "UK HRA - Research Summaries"),
                                    _ when external_url.Contains("cancerresearchuk.org") => new Tuple<int, string>(109371, "Cancer Research UK trial summaries"),
                                    _ when external_url.Contains("/osf.") => new Tuple<int, string>(101913, "OSF"),
                                    _ when external_url.Contains("servier.com") => new Tuple<int, string>(109373, "Servier repository"),
                                    _ when external_url.Contains("/10.1007") => new Tuple<int, string>(109374, "Springer Link"),
                                    _ when external_url.Contains("/10.1016") => new Tuple<int, string>(109375, "Elsevier Science Direct"),
                                    _ when external_url.Contains("/10.1093") => new Tuple<int, string>(109376, "Oxford Academic online journals"),
                                    _ when external_url.Contains("/10.1101") => new Tuple<int, string>(109377, "medRxiv pre-prints"),
                                    _ when external_url.Contains("/10.1111") => new Tuple<int, string>(109378, "Wiley Online"),
                                    _ when external_url.Contains("/10.1177") => new Tuple<int, string>(109379, "Sage online journals"),
                                    _ when external_url.Contains("/10.1186") => new Tuple<int, string>(109380, "BMC online journals"),
                                    _ when external_url.Contains("/10.1371") => new Tuple<int, string>(109381, "Plos One online journals"),
                                    _ when external_url.Contains("/10.3310") => new Tuple<int, string>(109385, "NIHR - Journals Library"),
                                    _ when external_url.Contains("/10.3389") => new Tuple<int, string>(109383, "Frontiers online journals'"),
                                    _ when external_url.Contains("/10.3390") => new Tuple<int, string>(109386, "MDPI online journals"),
                                    _ when external_url.Contains("/10.21203") => new Tuple<int, string>(109384, "Research Square online journals"),
                                    _  => new Tuple<int, string>(0, "")
                                };

                                int? system_id = null;
                                string? system = null;
                                if (system_details.Item1 != 0)
                                {
                                    system_id = system_details.Item1;
                                    system = system_details.Item2;
                                }
                                
                                // May be able to get repository org in some cases from the Urls
                                // or may wish to try to resolve the DOIs at some later point
                                
                                object_instances.Add(new ObjectInstance(sd_oid, system_id, system,
                                    external_url, true, 35, "Web text"));

                                if (dt_created is not null)
                                {
                                    object_dates.Add(new ObjectDate(sd_oid, 15, "Created", dt_created.year,
                                        dt_created.month,
                                        dt_created.day, dt_created.date_string));
                                }
                            }
                        }
                    }

                    if (artefact_type == "LocalFile" && !string.IsNullOrEmpty(local_url))
                    {
                        // some form of local file, stored on the ISRCTN web site

                        string? local_file_name = op.downloadFilename;
                        if (!string.IsNullOrEmpty(local_file_name))
                        {
                            string lower_name = local_file_name.ToLower();
                            int res_type_id = 0;
                            string res_type = "Not yet known";
                            if (lower_name.EndsWith(".pdf"))
                            {
                                res_type_id = 11;
                                res_type = "PDF";
                            }
                            else if (lower_name.EndsWith(".docx") || lower_name.EndsWith(".doc"))
                            {
                                res_type_id = 16;
                                res_type = "Word doc";
                            }
                            else if (lower_name.EndsWith(".pptx") || lower_name.EndsWith(".ppt"))
                            {
                                res_type_id = 20;
                                res_type = "PowerPoint";
                            }

                            if (lower_name.EndsWith(".pdf") || lower_name.EndsWith(".doc") ||
                                lower_name.EndsWith(".ppt"))
                            {
                                local_file_name = local_file_name[..^4];
                            }
                            
                            if (lower_name.EndsWith(".docx") || lower_name.EndsWith(".pptx"))
                            {
                                local_file_name = local_file_name[..^5];
                            }

                            object_display_title = s.display_title + " :: " + local_file_name;
                            sd_oid = sid + " :: " + object_type_id + " :: " + local_file_name;
                            int title_type_id = 21;
                            string title_type = "Study short name :: object name";

                            int next_num = checkOID(sd_oid, data_objects);
                            if (next_num > 0)
                            {
                                sd_oid += "_" + next_num;
                                object_display_title += "_" + next_num;
                            }

                            SplitDate? dt_created = null;
                            if (!string.IsNullOrEmpty(op.dateCreated))
                            {
                                dt_created = op.dateCreated?[..10].GetDatePartsFromISOString();
                            }

                            SplitDate? dt_available = null;
                            if (!string.IsNullOrEmpty(op.dateUploaded))
                            {
                                dt_available = op.dateUploaded?[..10].GetDatePartsFromISOString();
                            }

                            DataObject d_obj = new(sd_oid, sid, local_file_name, object_display_title, dt_created?.year,
                                    object_class_id, object_class, object_type_id, object_type, 100126, "ISRCTN", 11, download_datetime)
                                {
                                    version = op.version
                                };
                            data_objects.Add(d_obj);

                            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                    title_type_id, title_type, true));

                            object_instances.Add(new ObjectInstance(sd_oid, 100126, "ISRCTN",
                                    local_url, true, res_type_id, res_type));

                            if (dt_created is not null)
                            {
                                object_dates.Add(new ObjectDate(sd_oid, 15, "Created", dt_created.year, dt_created.month,
                                                                         dt_created.day, dt_created.date_string));
                            }
                            if (dt_available is not null)
                            {
                                object_dates.Add(new ObjectDate(sd_oid, 12, "Available", dt_available.year, dt_available.month,
                                                                        dt_available.day, dt_available.date_string));
                            }
                        }
                    }
                }
            }
        }
        
        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Construct final study object
        ///////////////////////////////////////////////////////////////////////////////////////

        s.identifiers = identifiers;
        s.titles = titles;
        s.organisations = orgs2;
        s.people = people2;
        s.references = references;
        s.topics = topics;
        s.features = features;
        s.sites = sites;
        s.countries = countries;
        s.conditions = conditions;
        s.iec = iec;

        s.data_objects = data_objects;
        s.object_titles = object_titles;
        s.object_dates = object_dates;
        s.object_instances = object_instances;

        return s;
    }
   
    private int checkOID(string sd_oid, List<DataObject> data_objects)
    {
        int next_num = 0;
        if (data_objects.Any())
        {
            foreach (DataObject d_o in data_objects)
            {
                if (d_o.sd_oid!.StartsWith(sd_oid))
                {
                    next_num++;
                }
            }
        }
        return next_num;
    }
}


