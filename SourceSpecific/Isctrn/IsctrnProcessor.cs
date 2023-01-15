using System.Globalization;
using System.Linq;
using System.Text.Json;
using MDR_Harvester.Euctr;
using MDR_Harvester.Extensions;
using MDR_Harvester.Isctrn;

namespace MDR_Harvester.Isrctn;

public class IsrctnProcessor : IStudyProcessor
{
    IMonitorDataLayer _mon_repo;
    LoggingHelper _logger_helper;

    public IsrctnProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger_helper)
    {
        _mon_repo = mon_repo;
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

        Study s = new Study();

        List<StudyIdentifier> identifiers = new List<StudyIdentifier>();
        List<StudyTitle> titles = new List<StudyTitle>();
        List<StudyContributor> contributors = new List<StudyContributor>();
        List<StudyReference> references = new List<StudyReference>();
        List<StudyTopic> topics = new List<StudyTopic>();
        List<StudyFeature> features = new List<StudyFeature>();
        List<StudyLocation> sites = new List<StudyLocation>();
        List<StudyCountry> countries = new List<StudyCountry>();

        List<DataObject> data_objects = new List<DataObject>();
        List<ObjectTitle> object_titles = new List<ObjectTitle>();
        List<ObjectDate> object_dates = new List<ObjectDate>();
        List<ObjectInstance> object_instances = new List<ObjectInstance>();

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

        s.brief_description = r.plainEnglishSummary;

        // study start date

        string? ss_date = r.overallStartDate;
        if (ss_date is not null)
        {
            SplitDate? study_start_date = ss_date[..10].GetDatePartsFromISOString();
            if (study_start_date is not null)
            {
                s.study_start_year = study_start_date.year;
                s.study_start_year = study_start_date.month;
            }
        }


        // study type
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

        if (string.IsNullOrEmpty(s.study_status))
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
        if (r_date is not null)
        {
            reg_date = r_date.Substring(0, 10).GetDatePartsFromISOString();
        }
        string? d_edited = r.lastUpdated;
        if (d_edited is not null)
        {
            last_edit = d_edited.Substring(0, 10).GetDatePartsFromISOString();
        }


        // Study sponsor(s) and funders.

        var sponsors = r.sponsors;
        string? sponsor_name = null;    // For later use, adding sponsor ids
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
            sponsor_name = contributors[0].organisation_name;
        }

        var funders = r.funders;
        if (funders?.Any() == true)
        {
            foreach (var funder in funders)
            {
                string? funder_name = funder.name;
                if (funder_name is not null && funder_name.AppearsGenuineOrgName())
                {
                    // check a funder is not simply the sponsor...(or repeated).

                    bool add_funder = true;
                    funder_name = funder_name.TidyOrgName(sid);
                    if (contributors.Count > 0)
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
                        contributors.Add(new StudyContributor(sid, 58, "Study Funder", null, funder_name));
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
                string? orcid = contact.orcid;
                if (orcid is not null && orcid.Contains("/"))
                {
                    orcid = orcid[(orcid.LastIndexOf("/") + 1)..];  // drop any url prefix
                }
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

                contributors.Add(new StudyContributor(sid, contrib_type_id, contrib_type, givenName,
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
                string? ivalue = ident.identifier_value?.Trim();
                if (!string.IsNullOrEmpty(ivalue))
                {
                    if (ivalue != "To be determned" && ivalue != "To be determined")
                    {
                        identifiers.Add(new StudyIdentifier(sid, ivalue, ident.identifier_type_id, ident.identifier_type,
                                                            ident.identifier_org_id, ident.identifier_org, null, null));
                    }
                    else
                    {
                        if (sponsor_name is not null)
                        {
                            // 'serial protocol number':  already split if included a ';' or ','

                            IsrctnIdentifierDetails idd = ih.GetISRCTNIdentifierProps(ivalue, sponsor_name);
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
        if (phase is not null && s.study_type_id == 11)
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

            features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                               new_feature.Item3, new_feature.Item4));
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

                if (new_feature.Item1 != 0)
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

                features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                                   new_feature.Item3, new_feature.Item4));
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

            features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                               new_feature.Item3, new_feature.Item4));

        }


        // Include listed drug or device names as topics.

        string? drugNames = r.drugNames;
        List<string> topic_names = new();
        if (!string.IsNullOrEmpty(drugNames) && drugNames != "N/A")
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
                    string[] split_names = drugNames.Split(',');
                    foreach (string sn in split_names)
                    {
                        topic_names.Add(sn);
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


        // Include conditions as topics.

        string? listed_condition = r.conditionDescription;
        if (listed_condition is not null)
        {
            topics.Add(new StudyTopic(sid, 13, "condition", listed_condition));
        }
        else
        {
            // These tend to be very general - high level classifcvations.
            // Often a comma delimited list.

            string? disease_class = r.diseaseClass1;
            if (disease_class is not null)
            {
                if (disease_class.Contains(","))
                {
                    // add topics
                    string[] conds = disease_class.Split(',');
                    for (int i = 0; i < conds.Length; i++)
                    {
                        topics.Add(new StudyTopic(sid, 13, "condition", conds[i]));
                    }
                }
                else
                {
                    topics.Add(new StudyTopic(sid, 13, "condition", disease_class));
                }
            }
        }


        // Eligibility.

        string? final_enrolment = r.totalFinalEnrolment;
        string? target_enrolment = r.targetEnrolment?.ToString();

        if (target_enrolment is not null && target_enrolment != "Not provided at time of registration")
        {
            s.study_enrolment = target_enrolment;
        }

        if (final_enrolment is not null && final_enrolment != "Not provided at time of registration")
        {
            if (s.study_enrolment is null)
            {
                s.study_enrolment = final_enrolment;
            }
            else
            {
                s.study_enrolment += " (planned), " + final_enrolment + " (final).";
            }
        }


        string? gender = r.gender; 
        if (gender is not null) 
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
        if (age_group is not null && age_group != "Mixed"
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

            if (age_params.Item1 is not null || age_params.Item3 is not null)
            {
                s.min_age = age_params.Item1;
                s.min_age_units = age_params.Item2;
                s.min_age_units_id = s.min_age_units.GetTimeUnitsId();
                s.max_age = age_params.Item3;
                s.max_age_units = age_params.Item4;
                s.max_age_units_id = s.max_age_units.GetTimeUnitsId();
            }
        }


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

        string? ipd_ss = r.ipdSharingStatement;
        if (ipd_ss is not null && ipd_ss != "Not provided at time of registration")
        {
            s.data_sharing_statement = ipd_ss;
        }

        // *********************************
        // add in data policies
        // *********************************
       

        // DATA OBJECTS and their attributes
        // initial data object is the ISRCTN registry entry

        int? pub_year = null;
        if (reg_date is not null)
        {
            pub_year = reg_date.year;
        }
        string object_title = "ISRCTN registry entry";
        string object_display_title = s.display_title + " :: ISRCTN registry entry";

        // create Id for the data object
        string sd_oid = sid + " :: 13 :: " + object_title;

        DataObject dobj = new DataObject(sd_oid, sid, object_title, object_display_title, pub_year,
                23, "Text", 13, "Trial Registry entry", 100126, "ISRCTN", 12, download_datetime);

        dobj.doi = r.doi;
        dobj.doi_status_id = 1;
        data_objects.Add(dobj);

        // data object title is the single display title...
        object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                                    22, "Study short name :: object type", true));
        if (last_edit != null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                                last_edit.year, last_edit.month, last_edit.day, last_edit.date_string));
        }

        if (reg_date != null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 15, "Created",
                                reg_date.year, reg_date.month, reg_date.day, reg_date.date_string));
        }

        // instance url can be derived from the ISRCTN number
        object_instances.Add(new ObjectInstance(sd_oid, 100126, "ISRCTN",
                    "https://www.isrctn.com/" + sid, true, 35, "Web text"));


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


        /*               
                        case "Individual participant data (IPD) sharing statement":
                            {
                                if (!item_value.Contains("Not provided at time of registration"))
                                {
                                    if (!string.IsNullOrEmpty(sharing_statement))
                                    {
                                        sharing_statement += "\n";
                                    }
                                    sharing_statement += sh.StringClean("IPD sharing statement: " + item_value);
                                }
                                break;
                            }
                        case "Participant level data":
                            {
                                if (!item_value.Contains("Not provided at time of registration"))
                                {
                                    if (!string.IsNullOrEmpty(sharing_statement))
                                    {
                                        sharing_statement += "\n";
                                    }
                                    sharing_statement += sh.StringClean("IPD Management: " + item_value);
                                }
*/


        // possible additional files

        // this source specific utility class defined immediately below this class
        // and reset to a new collection for each study

        List<AdditionalFile> additional_files = new List<AdditionalFile>();

        var add_files = r.Element("additional_files");
        if (add_files != null)
        {
            var items = add_files.Elements("Item");
            if (items != null && items.Count() > 0)
            {
                foreach (XElement item in items)
                {
                    string item_name = GetElementAsString(item.Element("item_name")).Trim();
                    string item_value = GetElementAsString(item.Element("item_value")).Trim();

                    // may need to correct an extraction error here...
                    item_value = item_value.Replace("//editorial", "/editorial");

                    additional_files.Add(new AdditionalFile(sid, item_name, item_value));
                }
            }
        }


        // outputs
        var outputs = r.Element("outputs");
        if (outputs != null)
        {
            var items = outputs.Elements("Output");
            if (items != null && items.Count() > 0)
            {
                foreach (XElement item in items)
                {
                    string output_type = GetElementAsString(item.Element("output_type")).Trim();
                    string output_url = GetElementAsString(item.Element("output_url")).Trim();
                    string details = GetElementAsString(item.Element("details")).Trim();

                    string date_created = GetElementAsString(item.Element("date_created")).Trim();
                    string date_added = GetElementAsString(item.Element("date_added")).Trim();

                    SplitDate created = null;
                    SplitDate added = null;
                    int? year_published = null;

                    if (!string.IsNullOrEmpty(date_created))
                    {
                        date_created = date_created.Substring(0, 10);
                        created = dh.GetDatePartsFromISOString(date_created);
                        year_published = created.year;
                    }
                    if (!string.IsNullOrEmpty(date_added))
                    {
                        date_added = date_added.Substring(0, 10);
                        added = dh.GetDatePartsFromISOString(date_added);
                    }

                    // correct a common past url error in download process
                    // and remove any trailing full stop, semi-colon or slash

                    if (output_url.StartsWith("https://www.isrctn.com/http"))
                    {
                        output_url = output_url.Substring(23);
                    }

                    if (output_url.EndsWith(";") || output_url.EndsWith(".")
                            || output_url.EndsWith("/"))
                    {
                        output_url = output_url.Substring(0, output_url.Length - 1);
                    }

                    // depends if they are an article, usually with a pubmed reference,
                    // or some other type of output

                    string output_lower = output_type.ToLower();
                    if (output_lower == "protocol article" || output_lower == "results article"
                        || output_lower == "interim results article" || output_lower == "preprint results"
                        || output_lower == "other publications" || output_lower == "abstract results"
                        || output_lower == "abstract results" || output_lower == "thesis results "
                        || output_lower == "thesis results" || output_lower == "protocol (preprint)"
                        || output_lower == "preprint (other)")
                    {

                        string doi = "", citation = "", pmid_string = "";
                        int pmid = 0;
                        bool pmid_found = false;

                        // try and get a pmid

                        if (output_url.Contains("pubmed"))
                        {
                            if (output_url.Contains("list_uids="))
                            {
                                string poss_pmid = output_url.Substring(output_url.IndexOf("list_uids=") + 10);
                                if (Int32.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else if (output_url.Contains("termtosearch="))
                            {
                                string poss_pmid = output_url.Substring(output_url.IndexOf("termtosearch=") + 13);
                                if (Int32.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else if (output_url.Contains("term="))
                            {
                                string poss_pmid = output_url.Substring(output_url.IndexOf("term=") + 5);
                                if (Int32.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }
                            else
                            {
                                // 'just' /puibmed_id at the end ...
                                string poss_pmid = output_url.Substring(output_url.LastIndexOf("/") + 1);
                                if (Int32.TryParse(poss_pmid, out pmid))
                                {
                                    pmid_found = true;
                                }
                            }

                            if (pmid_found && pmid > 0)
                            {
                                pmid_string = pmid.ToString();
                            }
                            else
                            {
                                citation = output_url;
                            }
                        }
                        else
                        {
                            // include the url in the citation field
                            citation = output_url;
                        }

                        // is there a doi?
                        if (output_url.Contains("doi"))
                        {
                            doi = output_url.Substring(output_url.IndexOf("doi.org/") + 8);
                        }

                        string comments = output_type.ToLower();

                        if (!string.IsNullOrEmpty(details.Trim()))
                        {
                            if ((comments == "protocol article"
                                        && details.ToLower() != "protocol" && details.ToLower() != "protocol")
                            || (comments == "results article"
                                        && details.ToLower() != "results"))
                            {
                                comments = comments + " (" + details + ")";
                            }
                        }

                        // add the details to the study references

                        references.Add(new StudyReference(sid, pmid_string, citation, doi, comments));
                    }
                    else
                    {
                        // need to correct a possible past extraction error here...
                        output_url = output_url.Replace("//editorial", "/editorial");

                        // create object details
                        string object_type = "";
                        int object_type_id;
                        string object_class = "Text";
                        int object_class_id = 23;

                        // One of several data object types - usually as stored by ISRCTN
                        if (output_lower == "basic results" || output_lower == "funder report results"
                            || output_lower == "thesis results" || output_lower == "poster results"
                            || output_lower == "other unpublished results" || output_lower == "book results")
                        {
                            object_type_id = 79;
                            object_type = "Results or CSR summary";
                        }
                        else if (output_lower == "protocol file" || output_lower == "protocol (other)")
                        {
                            object_type_id = 11;
                            object_type = "Study Protocol";
                        }
                        else if (output_lower == "participant information sheet")
                        {
                            object_type_id = 19;
                            object_type = "Patient information sheets";
                        }
                        else if (output_lower == "dataset")
                        {
                            object_type_id = 80;
                            object_type = "Individual participant data";
                            object_class_id = 14;
                            object_class = "Dataset";
                        }
                        else if (output_lower == "plain english results")
                        {
                            object_type_id = 88;
                            object_type = "Summary of results for public";
                        }
                        else if (output_lower.Contains("analysis"))
                        {
                            object_type_id = 22;
                            object_type = "Statistical analysis plan";
                        }
                        else if (output_lower.Contains("consent"))
                        {
                            object_type_id = 18;
                            object_type = "Informed consent forms";
                        }
                        else if (output_lower == "other files")
                        {
                            object_type_id = 37;
                            object_type = "Other text based object";
                        }
                        else if (output_lower == "trial website")
                        {
                            object_type_id = 134;
                            object_type = "Website";
                        }
                        else
                        {
                            object_type_id = 37;
                            object_type = "Other text based object";
                        }

                        // does this object exist in the additional files list - it should do...
                        // but may not be the case. If it does get the name

                        string specific_object_name = "";
                        if (additional_files.Count > 0)
                        {
                            foreach (AdditionalFile af in additional_files)
                            {
                                if (output_url == af.item_value)
                                {
                                    specific_object_name = af.item_name;
                                    break;
                                }
                            }
                        }

                        int res_type_id = 0;
                        string res_type = "Not yet known";
                        int title_type_id = 0;
                        string title_type = "Not yet known";

                        if (specific_object_name == "")
                        {
                            // may be able to derive a document title from the url
                            string url_lower = output_url.ToLower();
                            if (url_lower.Contains(".pdf"))
                            {
                                int file_suffix_pos = url_lower.IndexOf(".pdf");
                                int name_start_pos = output_url.LastIndexOf('/', file_suffix_pos);
                                specific_object_name = output_url.Substring(name_start_pos + 1, file_suffix_pos + 3 - name_start_pos);
                            }
                            else if (url_lower.Contains(".docx"))
                            {
                                int file_suffix_pos = url_lower.IndexOf(".docx");
                                int name_start_pos = output_url.LastIndexOf("/", file_suffix_pos);
                                specific_object_name = output_url.Substring(name_start_pos + 1, file_suffix_pos + 4 - name_start_pos);
                            }
                            else if (url_lower.Contains(".doc"))
                            {
                                int file_suffix_pos = url_lower.IndexOf(".doc");
                                int name_start_pos = output_url.LastIndexOf("/", file_suffix_pos);
                                specific_object_name = output_url.Substring(name_start_pos + 1, file_suffix_pos + 3 - name_start_pos);
                            }
                            else if (url_lower.Contains(".pptx"))
                            {
                                int file_suffix_pos = url_lower.IndexOf(".pptx");
                                int name_start_pos = output_url.LastIndexOf("/", file_suffix_pos);
                                specific_object_name = output_url.Substring(name_start_pos + 1, file_suffix_pos + 4 - name_start_pos);
                            }
                            else if (url_lower.Contains(".ppt"))
                            {
                                int file_suffix_pos = url_lower.IndexOf(".ppt");
                                int name_start_pos = output_url.LastIndexOf("/", file_suffix_pos);
                                specific_object_name = output_url.Substring(name_start_pos + 1, file_suffix_pos + 3 - name_start_pos);
                            }
                        }


                        if (specific_object_name == "")
                        {
                            object_title = object_type;
                            object_display_title = s.display_title + " :: " + object_type;
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_type;
                            title_type_id = 22;
                            title_type = "Study short name :: object type";

                            // in almost all cases the lack of a matching document or embedded document
                            // is because the material is provided as a web page

                            if (output_url.StartsWith("http"))
                            {
                                res_type_id = 35;
                                res_type = "Web text";
                            }

                            // need to check if the sd_oid a duplicate?
                            // probably not as most objects should have a specific name
                        }
                        else
                        {
                            if (specific_object_name.ToLower().EndsWith(".pdf"))
                            {
                                res_type_id = 11;
                                res_type = "PDF";
                                specific_object_name = specific_object_name.Substring(0, specific_object_name.LastIndexOf("."));
                            }
                            else if (specific_object_name.ToLower().EndsWith(".docx") || specific_object_name.ToLower().EndsWith(".doc"))
                            {
                                res_type_id = 16;
                                res_type = "Word doc";
                                specific_object_name = specific_object_name.Substring(0, specific_object_name.LastIndexOf("."));

                            }
                            else if (specific_object_name.ToLower().EndsWith(".pptx") || specific_object_name.ToLower().EndsWith(".ppt"))
                            {
                                res_type_id = 20;
                                res_type = "PowerPoint";
                                specific_object_name = specific_object_name.Substring(0, specific_object_name.LastIndexOf("."));
                            }

                            object_title = specific_object_name;
                            object_display_title = s.display_title + " :: " + specific_object_name;
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + specific_object_name;
                            title_type_id = 21;
                            title_type = "Study short name :: object name";
                        }


                        // do a check that the sd_oid and resulting object name is not a duplicate
                        // if it is add a suffix before making the addition

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

                        if (next_num > 0)
                        {
                            sd_oid += "_" + next_num.ToString();
                            object_display_title += "_" + next_num.ToString();
                        }

                        DataObject new_dobj = new DataObject(sd_oid, sid, object_title, object_display_title, year_published,
                                    object_class_id, object_class, object_type_id, object_type, 100126, "ISRCTN", 11, download_datetime);

                        if (details.ToLower().StartsWith("version"))
                        {
                            new_dobj.version = details;
                        }

                        data_objects.Add(new_dobj);

                        object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                    title_type_id, title_type, true));
                        object_instances.Add(new ObjectInstance(sd_oid, 100126, "ISRCTN",
                                output_url, true, res_type_id, res_type));

                        if (created != null)
                        {
                            object_dates.Add(new ObjectDate(sd_oid, 15, "Created", created.year, created.month, created.day, created.date_string));
                        }
                        if (added != null)
                        {
                            object_dates.Add(new ObjectDate(sd_oid, 11, "Accepted", added.year, added.month, added.day, added.date_string));
                        }
                    }
                }
            }
        }


        // possible object of a trial web site if one exists for this study

        string trial_website = GetElementAsString(r.Element("trial_website"));
        if (!string.IsNullOrEmpty(trial_website))
        {
            // first check website link does not provide a 404
            if (true) //await HtmlHelpers.CheckURLAsync(fs.trial_website))
            {
                object_title = "Study web site";
                object_display_title = s.display_title + " :: Study web site";
                sd_oid = sid + " :: 134 :: " + object_title;

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, s.study_start_year,
                        23, "Text", 134, "Website", null, sponsor_name, 12, download_datetime));
                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                                        22, "Study short name :: object type", true));
                ObjectInstance instance = new ObjectInstance(sd_oid, null, study_sponsor,
                        trial_website, true, 35, "Web text");
                instance.url_last_checked = DateTime.Today;
                object_instances.Add(instance);
            }
        }


        s.brief_description = study_description;
        s.data_sharing_statement = sharing_statement;

        s.identifiers = identifiers;
        s.titles = titles;
        s.contributors = contributors;
        s.references = references;
        s.topics = topics;
        s.features = features;
        s.sites = sites;
        s.countries = countries;

        s.data_objects = data_objects;
        s.object_titles = object_titles;
        s.object_dates = object_dates;
        s.object_instances = object_instances;

        return s;

    }
}


