using System.Globalization;
using System.Text.Json;
using MDR_Harvester.Extensions;
using MDR_Harvester.Isctrn;
using Microsoft.Extensions.FileProviders.Physical;

namespace MDR_Harvester.Isrctn;

public class IsrctnProcessor : IStudyProcessor
{
    private readonly ILoggingHelper _logger_helper;

    public IsrctnProcessor(ILoggingHelper logger_helper)
    {
        _logger_helper = logger_helper;
    }

    public Study? ProcessData(string json_string, DateTime? download_datetime)
    {
        // set up json reader and deserialise file to a ISCTRN_Record object.

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };


        ISCTRN_Record? r = JsonSerializer.Deserialize<ISCTRN_Record?>(json_string, json_options);
        if (r is null)
        {
            _logger_helper.LogError($"Unable to deserialise json file to Euctr_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }

        Study s = new();

        List<StudyIdentifier> identifiers = new();
        List<StudyTitle> titles = new();
        List<StudyContributor> contributors = new();
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

        string? sid = r.sd_sid;

        if (string.IsNullOrEmpty(sid))
        {
            _logger_helper.LogError($"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        // get basic study attributes

        string? study_name = r.title;
        if (!string.IsNullOrEmpty(study_name))
        {
            s.display_title = study_name.ReplaceApos(); // = public title, default
            titles.Add(new StudyTitle(sid, s.display_title, 15, "Registry public title", true, "From ISRCTN"));
        }

        if (!string.IsNullOrEmpty(r.scientificTitle))
        {
            string sci_title = r.scientificTitle.ReplaceApos()!;
            if (s.display_title is null)
            {
                s.display_title = sci_title;
            }
            titles.Add(new StudyTitle(sid, sci_title, 16, "Registry scientific title", s.display_title == sci_title, "From ISRCTN"));
        }

        if (!string.IsNullOrEmpty(r.acronym))
        {
            if (s.display_title is null)
            {
                s.display_title = r.acronym;
            }
            titles.Add(new StudyTitle(sid, r.acronym, 14, "Acronym or Abbreviation", s.display_title == r.acronym, "From ISRCTN"));
        }

        // Brief description.
        // From Plain Englilsh Summary if one available
        // Otherwise try to use the study hypothesis and primary outcome, if available

        s.brief_description = r.plainEnglishSummary;
        if (string.IsNullOrEmpty(s.brief_description) 
            || s.brief_description.ToLower().StartsWith("not provided"))
        {
            string hypothesis = r.studyHypothesis.StringClean() ?? "";
            string poutcome = r.primaryOutcome.StringClean() ?? "";
            if (hypothesis != "" && !hypothesis.ToLower().StartsWith("not provided"))
            {
                if (!hypothesis.ToLower().StartsWith("hypothes") && !hypothesis.ToLower().StartsWith("study hyp"))
                {
                    hypothesis = "Study hypothesis: " + hypothesis;
                }
                s.brief_description = hypothesis ?? "";
            }
            if (poutcome != "" && !poutcome.ToLower().StartsWith("not provided"))
            {
                if (!poutcome.ToLower().StartsWith("primary") && !poutcome.ToLower().StartsWith("outcome"))
                {
                    poutcome = "Primary outcome: " + poutcome;
                }
                s.brief_description += s.brief_description == "" ? poutcome : "\n" + poutcome;
            }
        }

        // Study start date.

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

        // Study type and status.

        s.study_type = r.primaryStudyDesign;
        s.study_type_id = s.study_type.GetTypeId();

        // Study status from overall study status or more commonly from dates.
        // 'StatusOverride' field will only have a value if status is
        // 'Suspended' or 'Stopped'.
        // More commonly compare dates with today to get current status.
        // Means periodic full import or a separate mechanism to update 
        // statuses against dates.
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
                            if (rs_date_dt > DateTime.Now)
                            {
                                s.study_status = "Not yet recruiting";
                            }
                            else
                            {
                                s.study_status = "Recruiting";
                            }
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


        // Study sponsor(s) and funders.

        var sponsors = r.sponsors;
        string? sponsor_name = null;    // For later use
        if (sponsors?.Any() == true)
        {
            foreach (var stSponsor in sponsors)
            {
                string? org = stSponsor.organisation;
                if (org.AppearsGenuineOrgName())
                {
                    string? orgname = org.TidyOrgName(sid);
                    contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", null, orgname));
                }
            }
            if (contributors.Any() == true)
            {
                sponsor_name = contributors[0].organisation_name;
            }
        }

        var funders = r.funders;
        if (funders?.Any() == true)
        {
            foreach (var funder in funders)
            {
                string? funder_name = funder.name;
                if (!string.IsNullOrEmpty(funder_name) && funder_name.AppearsGenuineOrgName())
                {
                    // check a funder is not simply the sponsor...(or repeated).

                    bool add_funder = true;
                    funder_name = funder_name.TidyOrgName(sid);
                    if (contributors!.Count > 0)
                    {
                        foreach (var c in contributors)
                        {
                            if (funder_name == c.organisation_name)
                            {
                                add_funder = false;
                                break;
                            }
                        }
                    }

                    if (add_funder)
                    {
                        contributors!.Add(new StudyContributor(sid, 58, "Study Funder", null, funder_name));
                    }
                }
            }
        }

        // Individual contacts.

        var contacts = r.contacts;
        if (contacts?.Any() == true)
        {
            foreach (var contact in contacts)
            {
                string? cType = contact.contactType;
                string? givenName = contact.forename.TidyPersonName();
                string? familyName = contact.surname.TidyPersonName();
                string? affil = contact.address;
                string? orcid = contact.orcid.TidyORCIDId();
                string full_name = (givenName ?? "" + " " + familyName ?? "").Trim();

                int contrib_type_id;
                string? contrib_type;
                if (cType == "Scientific" || cType == "Principal Investigator")
                {
                    contrib_type_id = 51;
                    contrib_type = "Study Lead";
                }
                else if (cType == "Public")
                {
                    contrib_type_id = 56;
                    contrib_type = "Public contact";
                }
                else
                {
                    contrib_type_id = 0;
                    contrib_type = cType;
                }

                contributors?.Add(new StudyContributor(sid, contrib_type_id, contrib_type, givenName,
                                                        familyName, full_name, orcid, affil));
            }
        }

        // Try to ensure contributors are properly categorised.
        // Check if a group has been inserted as an individual,
        // or an individual has been inserted as a group.

        if (contributors.Count > 0)
        {
            foreach (StudyContributor sc in contributors)
            {
                if (sc.is_individual == true)
                {
                    if (sc.person_full_name.IsAnOrganisation())
                    {
                        sc.organisation_name = sc.person_full_name.TidyOrgName(sid);
                        sc.person_full_name = null;
                        sc.is_individual = false;
                    }
                }

                if (sc.is_individual == false)
                {
                    if (sc.organisation_name.IsAnIndividual())
                    {
                        sc.person_full_name = sc.organisation_name.TidyPersonName();
                        sc.organisation_name = null;
                        sc.is_individual = true;
                    }
                }
            }
        }


        // Study identifiers - do the isrctn id first...
        // then any others that might be listed.

        identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100126, "ISRCTN", reg_date?.date_string, null));

        var idents = r.identifiers;
        if (idents?.Any() == true)
        {
            foreach (var ident in idents)
            {
                string? iType = ident.identifier_type?.Trim();
                if (!string.IsNullOrEmpty(iType) && !string.IsNullOrEmpty(ident.identifier_value))
                {
                    if (iType != "To be determined" && iType != "To be determned")
                    {
                        identifiers.Add(new StudyIdentifier(sid, ident.identifier_value, ident.identifier_type_id, iType,
                                                            ident.identifier_org_id, ident.identifier_org, null, null));
                    }
                    else
                    {
                        if (sponsor_name is not null)
                        {
                            // 'serial protocol number':  already split if included a ';' or ','

                            IsrctnIdentifierDetails idd = ih.GetISRCTNIdentifierProps(ident.identifier_value, sponsor_name);
                            if (idd.id_type != "Not usable" && idd.id_value.IsNewToList(identifiers))
                            {
                                identifiers.Add(new StudyIdentifier(sid, idd.id_value, idd.id_type_id, idd.id_type,
                                                                       idd.id_org_id, idd.id_org, null, null));
                            }
                        }
                    }
                }
            }
        }


        // Design info and study features.
        // First provide phase for interventional trials.

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

        // Other features can be found in secondary design and / or study design fields.
        // Concatenate these before searching them.
        // Interventional study features considered first,
        // then observational study features

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


        // Include listed drug or device names as topics.

        List<string> topic_names = new();

        string? drugNames = r.drugNames;
        if (!string.IsNullOrEmpty(drugNames) && drugNames != "N/A"
            && !drugNames.ToLower().StartsWith("the sponsor has confirmed")
            && !drugNames.ToLower().StartsWith("the health research authority (hra) has approved"))
        {
            drugNames = drugNames.Replace("\u00AE", string.Empty); //  lose (r) Registration mark
            drugNames = drugNames.Replace("\u2122", string.Empty); //  lose (tm) Trademark mark

            if (drugNames.Contains("1.") && drugNames.Contains("2."))
            {
                // Numbered list (almost certainly) - split and add list

                List<string> numbered_strings = drugNames.GetNumberedStrings(".", 8);
                topic_names.AddRange(numbered_strings);
            }
            else if (r.interventionType == "Drug" || r.interventionType == "Supplement")
            {
                // if there are commas split on the commas (does not work for devices).

                if (drugNames.Contains(','))
                {
                    List<string>? split_drug_names = drugNames.SplitStringWithMinWordSize(',', 4);
                    if (split_drug_names is not null)
                    {
                        topic_names.AddRange(split_drug_names);
                    }
                }
            }
            else
            {
                topic_names.Add(drugNames);
            }
        }

        if (topic_names.Count > 0)
        {
            string topic_type = r.interventionType == "Device" ? "Device" : "Chemical / agent";
            int topic_type_id = r.interventionType == "Device" ? 21 : 12;
            foreach (string tn in topic_names)
            {
                topics.Add(new StudyTopic(sid, topic_type_id, topic_type, tn));
            }
        }


        // Conditions.

        string? listed_condition = r.conditionDescription;
        if (string.IsNullOrEmpty(listed_condition))
        {
            listed_condition = r.diseaseClass1;
        }
       
        if (!string.IsNullOrEmpty(listed_condition))
        {
            // Can be very general - high level classifications.
            // Often a delimited list.
            List<string> conds = new();
            
            if (listed_condition.Contains(","))
            {
                string[] cons = listed_condition.Split(',');
                for (int i = 0; i < cons.Length; i++)
                {
                    conds.Add(cons[i]);
                }
            }
            else if (listed_condition.Contains(";"))
            {
                // add condition
                string[] cons = listed_condition.Split(';');
                for (int i = 0; i < cons.Length; i++)
                {
                    conds.Add(cons[i]);
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
                conditions.Add(new StudyCondition(sid, listed_condition, null, null));
            }

            foreach (string cond1 in conds)
            {
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
                        conditions.Add(new StudyCondition(sid, cond, null, null));
                    }
                }
            }
        }


        // Eligibility.

        string? final_enrolment = r.totalFinalEnrolment;
        string? target_enrolment = r.targetEnrolment?.ToString();

        if (!string.IsNullOrEmpty(target_enrolment) && target_enrolment != "Not provided at time of registration")
        {
            s.study_enrolment = target_enrolment + " (target)";
        }

        if (!string.IsNullOrEmpty(final_enrolment) && final_enrolment != "Not provided at time of registration")
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

            if (age_params.Item1 is not null || age_params.Item1 is not null)
            {
                s.min_age = age_params.Item1;
                s.min_age_units = age_params.Item2;
                s.min_age_units_id = s.min_age_units.GetTimeUnitsId();
                s.max_age = age_params.Item3;
                s.max_age_units = age_params.Item4;
                s.max_age_units_id = s.max_age_units.GetTimeUnitsId();
            }
        }


        // Inclusion / Exclusion Criteria

        string? ic = r.inclusion;
        string? ec = r.exclusion;
        int study_iec_type = 0;
        
        if (!string.IsNullOrEmpty(ic))
        {
            List<Criterion>? crits = ih.GetNumberedCriteria(sid, ic, "inclusion");
            if (crits is not null)
                
            {
                int seq_num = 0;
                foreach (Criterion cr in crits)
                {    
                     seq_num++;
                     iec.Add(new StudyIEC(sid, seq_num, cr.CritTypeId, cr.CritType, cr.CritText));
                }
                study_iec_type = (crits.Count == 1) ? 2 : 4;
            }
        }

        if (!string.IsNullOrEmpty(ec))
        {
            List<Criterion>? crits = ih.GetNumberedCriteria(sid, ec, "exclusion");
            if (crits is not null)
            {
                int seq_num = 100;
                foreach (Criterion cr in crits)
                {
                    seq_num++;
                    iec.Add(new StudyIEC(sid, seq_num, cr.CritTypeId, cr.CritType, cr.CritText));
                }
                study_iec_type += (crits.Count == 1) ? 5 : 6;
            }
        }

        s.iec_level = study_iec_type;


        // Locations.
        // Countries have already been renamed and checked for duplication
        // as part of the download process

        var country_list = r.recruitmentCountries;
        if (country_list?.Any() == true)
        {
            foreach (string c in country_list)
            {
                countries.Add(new StudyCountry(sid, c));
            }
        }

        var locations = r.centres;
        if (locations?.Any() == true)
        {
            foreach (var loc in locations)
            {
                sites.Add(new StudyLocation(sid, loc.name));
            }
        }


        // Data Sharing.
        // Given by the data sharing statement and any data policies.
        // At the moment these seem to be a single string summarising
        // the management of IPD.

        string? ipd_ss = r.ipdSharingStatement;
        if (ipd_ss is not null && ipd_ss != "Not provided at time of registration")
        {
            s.data_sharing_statement = ipd_ss;
        }
        var data_policies = r.dataPolicies;
        if (data_policies?.Any() == true)
        {
            foreach (string policy in data_policies)
            {
                if (policy != "Not provided at time of registration")
                {
                    s.data_sharing_statement += "\nIPD policy summary: " + policy;
                }
            }
        }
               
        
       // DATA OBJECTS and their attributes
       // initial data object is the ISRCTN registry entry

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
                23, "Text", 13, "Trial Registry entry", 100126, "ISRCTN", 12, download_datetime);

        dobj.doi = r.doi;
        dobj.doi_status_id = 1;
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
                    "https://www.isrctn.com/" + sid, true, 35, "Web text"));


        // PIS details seem to havbe been largely transferred
        // to the 'Additional files' section.

        string? PIS_details = r.patientInfoSheet;
        if (PIS_details is not null && !PIS_details.StartsWith("Not available") 
             && !PIS_details.StartsWith("Not applicable") && PIS_details != "See additional files")
        {
            if (PIS_details.Contains("<a href"))
            {
                // PIS note includes an href to a web address
                int ref_start = PIS_details.IndexOf("href=") + 6;
                int ref_end = PIS_details.IndexOf("\"", ref_start + 1);
                string href = PIS_details[ref_start..ref_end];

                // first check link does not provide a 404 - to be re-implemented
                if (true) //await HtmlHelpers.CheckURLAsync(href))
                {
                    int res_type_id = 35;
                    string res_type = "Web text";
                    if (href.ToLower().EndsWith("pdf"))
                    {
                        res_type_id = 11;
                        res_type = "PDF";
                    }
                    else if (href.ToLower().EndsWith("docx") || href.ToLower().EndsWith("doc"))
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


        // Possible trial web site

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


        // Possible additional files and external links. Output list appears to be composed of both
        // external links to published papers and local files of unpublished supplementary material.
        // External links may be published papers - almost always referred to by pubmed ids, or links to 
        // web sites or unpublished papers with different types of information on them.
        // Local filers are often PIS, but may be result summaries and oher objects.

        var outputs = r.outputs;
        if(outputs?.Any() == true)
        {
            foreach (var op in outputs)
            {
                string? output_type = op.outputType;
                if (!string.IsNullOrEmpty(output_type))
                {
                    string output_lower = output_type.ToLower();
                    Tuple<int, string, int, string> object_details = output_lower switch
                    {
                        "resultsarticle" => new Tuple<int, string, int, string>(23, "Text", 202, "Journal article - results"),
                        "interimresiults" => new Tuple<int, string, int, string>(23, "Text", 203, "Journal article - interim results"),
                        "protocolarticle" => new Tuple<int, string, int, string>(23, "Text", 201, "Journal article - protocol"),
                        "funderreport" => new Tuple<int, string, int, string>(23, "Text", 204, "Journal article - review"),
                        "preprint" => new Tuple<int, string, int, string>(23, "Text", 210, "Preprint article"),
                        "otherpublications" => new Tuple<int, string, int, string>(23, "Text", 12, "Journal article - unspecified"),
                        "patient information sheet" => new Tuple<int, string, int, string>(23, "Text", 19, "Patient information sheets"),
                        "pis" or
                        "participant information sheet" or
                        "patient information sheet" => new Tuple<int, string, int, string>(23, "Text", 19, "Patient information sheets"),
                        "basicresults" or "thesis" or
                        "poster" or "otherunpublishedresults" or
                        "bookresults" or "abstract" => new Tuple<int, string, int, string>(23, "Text", 79, "Results or CSR summary"),
                        "protocolfile" or
                        "protocolother" => new Tuple<int, string, int, string>(23, "Text", 11, "Study Protocol"),
                        "dataset" => new Tuple<int, string, int, string>(14, "Dataset", 80, "Individual participant data"),
                        "plainenglishresults" => new Tuple<int, string, int, string>(23, "Text", 88, "Summary of results for public"),
                        "sap" => new Tuple<int, string, int, string>(23, "Text", 22, "Statistical analysis plan"),
                        _ when output_lower.Contains("analysis") => new Tuple<int, string, int, string>(23, "Text", 22, "Statistical analysis plan"),
                        _ when output_lower.Contains("consent") => new Tuple<int, string, int, string>(23, "Text", 18, "Informed consent forms"),
                        "otherfiles" => new Tuple<int, string, int, string>(23, "Text", 37, "Other text based object"),
                        "trialwebsite" => new Tuple<int, string, int, string>(23, "Text", 134, "Website"),
                        _ => new Tuple<int, string, int, string>(0, "Text", 0, output_lower),
                    };

                    int object_class_id = object_details.Item1;
                    string object_class = object_details.Item2;
                    int object_type_id = object_details.Item3;
                    string object_type = object_details.Item4;

                    string? artefact_type = op.artefactType;
                    string? external_url = op.externalLinkURL;
                    string? local_url = op.localFileURL;

                    if (artefact_type == "ExternalLink" && !string.IsNullOrEmpty(external_url))
                    {
                        string citation = external_url; // for storage 'as is' for later inspection
                        if (external_url.ToLower().Contains("pubmed"))
                        {
                            // Some sort of reference to a published article
                            // Tidy up url and try and get a pmid

                            int pmid = 0;
                            bool pmid_found = false;
                            char[] endcharsToTrim = new char[] { ';', '.', '/', '?' };
                            external_url = external_url.TrimEnd(endcharsToTrim);

                            if (external_url.Contains("list_uids="))
                            {
                                string poss_pmid = external_url[(external_url.IndexOf("list_uids=") + 10)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else if (external_url.Contains("termtosearch="))
                            {
                                string poss_pmid = external_url[(external_url.IndexOf("termtosearch=") + 13)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else if (external_url.Contains("term="))
                            {
                                string poss_pmid = external_url[(external_url.IndexOf("term=") + 5)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else
                            {
                                // 'just' /<pubmed_id> at the end ...
                                string poss_pmid = external_url[(external_url.LastIndexOf("/") + 1)..];
                                if (int.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
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
                            // A reference to an unpublished article, or an
                            // article in an non pubmed journmal, or a web-page
                            // Add as a 'Web text' object.
                            
                            if (external_url.Contains("doi"))
                            {
                                string doi = "";
                                if (external_url.IndexOf("doi.org/") != -1)
                                {
                                    doi = external_url[(external_url.IndexOf("doi.org/") + 8)..];
                                }
                                else if (external_url.IndexOf("/10.") != -1)
                                {
                                    doi = external_url[(external_url.IndexOf("/10.") + 1)..];
                                }
                            }

                            // Almost certainly an object / resource within a web page or as an
                            // unpublished web document. Create a data object record of this type,
                            // but ignore results records likely to be found in other sources.

                            if (!external_url.Contains("www.clinicaltrialsregister.eu") &&
                                !external_url.Contains("www.clinicaltrialsregister.eu"))
                            {
                                string object_name = object_type;
                                if (!string.IsNullOrEmpty(op.version))
                                {
                                    object_name += op.version;
                                }

                                object_display_title = s.display_title + " :: " + object_name;
                                sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_type;

                                int next_num = checkOID(sd_oid, data_objects);
                                if (next_num > 0)
                                {
                                    sd_oid += "_" + next_num.ToString();
                                    object_display_title += "_" + next_num.ToString();
                                }

                                SplitDate? dt_created = null;
                                if (!string.IsNullOrEmpty(op.dateCreated))
                                {
                                    dt_created = op.dateCreated?[..10].GetDatePartsFromISOString();
                                }

                                DataObject d_obj = new(sd_oid, sid, object_type, object_display_title,
                                    dt_created?.year,
                                    object_class_id, object_class, object_type_id, object_type, 100126, "ISRCTN",
                                    11,
                                    download_datetime);
                                d_obj.version = op.version;
                                
                                string doi = "";
                                if (external_url.Contains("doi"))
                                {
                                    if (external_url.IndexOf("doi.org/") != -1)
                                    {
                                        doi = external_url[(external_url.IndexOf("doi.org/") + 8)..];
                                    }
                                    else if (external_url.IndexOf("/10.") != -1)
                                    {
                                        doi = external_url[(external_url.IndexOf("/10.") + 1)..];
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

                                // May be able to get repository org in some cases from the Urls
                                // or may wish to try to resolve the dois at some later point
                                
                                object_instances.Add(new ObjectInstance(sd_oid, null, null,
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

                        string? localfile_name = op.downloadFilename;
                        if (!string.IsNullOrEmpty(localfile_name))
                        {
                            string lower_name = localfile_name.ToLower();
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

                            object_display_title = s.display_title + " :: " + localfile_name;
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + localfile_name;
                            int title_type_id = 21;
                            string title_type = "Study short name :: object name";

                            int next_num = checkOID(sd_oid, data_objects);
                            if (next_num > 0)
                            {
                                sd_oid += "_" + next_num.ToString();
                                object_display_title += "_" + next_num.ToString();
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

                            DataObject d_obj = new(sd_oid, sid, localfile_name, object_display_title, dt_created?.year,
                                    object_class_id, object_class, object_type_id, object_type, 100126, "ISRCTN", 11, download_datetime);
                            d_obj.version = op.version;
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

        s.identifiers = identifiers;
        s.titles = titles;
        s.contributors = contributors;
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
                if (d_o.sd_oid.StartsWith(sd_oid))
                {
                    next_num++;
                }
            }
        }
        return next_num;
    }
}


