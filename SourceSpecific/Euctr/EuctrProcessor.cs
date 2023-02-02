using System.Globalization;
using System.Text.Json;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Euctr;

public class EUCTRProcessor : IStudyProcessor
{
    public Study? ProcessData(string json_string, DateTime? download_datetime, ILoggingHelper _logging_helper)
    {
        // set up json reader and deserialise file to a BioLiNCC object.

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
        List<StudyPerson> people = new();
        List<StudyTopic> topics = new();
        List<StudyFeature> features = new();
        List<StudyCountry> countries = new();
        List<StudyCondition> conditions = new();
        List<StudyIEC> iec = new();

        List<DataObject> data_objects = new();
        List<ObjectTitle> object_titles = new();
        List<ObjectInstance> object_instances = new();
        List<ObjectDate> object_dates = new();
        
        IECHelpers iech = new();
        
        string? sid = r.sd_sid;

        if (string.IsNullOrEmpty(sid))
        {
            _logging_helper.LogError(
                $"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        // By definition with the EU CTR. all studies
        // are interventional trials. Status must be
        // derived.

        s.study_type = "Interventional";
        s.study_type_id = 11;

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
            s.study_status_id = new_status.Item1;
            s.study_status = new_status.Item2;
        }

        // study start year and month
        // public string start_date { get; set; } in ISO yyyy-MM-dd format.

        string? start_date = r.start_date;
        if (!string.IsNullOrEmpty(start_date) &&
            DateTime.TryParseExact(start_date, "yyyy-MM-dd", new CultureInfo("en-UK"), DateTimeStyles.AssumeLocal,
                out DateTime start))
        {
            s.study_start_year = start.Year;
            s.study_start_month = start.Month;
        }

        // contributor - sponsor.

        string? sponsor_name = "No organisation name provided in source data";
        string? sponsor = r.sponsor_name;
        if (sponsor.AppearsGenuineOrgName())
        {
            sponsor_name = sponsor?.TidyOrgName(sid);
            string? lc_sponsor = sponsor_name?.ToLower();
            if (!string.IsNullOrEmpty(lc_sponsor) && lc_sponsor.Length > 1
                                                  && lc_sponsor != "dr" && lc_sponsor != "no profit")
            {
                organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", null, sponsor_name));
            }
        }

        // may get funders or other supporting organisations.

        var sponsors = r.sponsors;
        if (sponsors?.Any() is true)
        {
            foreach (var dline in sponsors)
            {
                string? item_name = dline.item_name;
                if (item_name == "Name of organisation providing support")
                {
                    var values = dline.item_values;
                    if (values?.Any() is true)
                    {
                        string? org_value = values[0].value;
                        if (org_value.AppearsGenuineOrgName())
                        {
                            string? funder = org_value?.TidyOrgName(sid);
                            if (funder != sponsor_name)
                            {
                                // Check a funder is not simply the sponsor.

                                string? lc_funder = funder?.ToLower();
                                if (!string.IsNullOrEmpty(lc_funder) && lc_funder.Length > 1
                                                                     && lc_funder != "dr" && lc_funder != "no profit")
                                {
                                    organisations.Add(new StudyOrganisation(sid, 58, "Study Funder", null, funder));
                                }
                            }
                        }
                    }
                }
            }
        }


        // Study identifiers - 
        // do the euctr id first... then do the sponsor's id.

        identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100123, "EU Clinical Trials Register",
            null, null));

        string? sponsor_id = r.sponsor_id;
        if (!string.IsNullOrEmpty(sponsor_id))
        {
            string sp_name = !string.IsNullOrEmpty(sponsor_name)
                ? sponsor_name
                : "No organisation name provided in source data";
            int? sp_id = !string.IsNullOrEmpty(sponsor_name) ? null : 12;
            identifiers.Add(new StudyIdentifier(sid, sponsor_id, 14, "Sponsor ID", sp_id, sp_name, null, null));
        }


        // Identifier section includes titles.

        bool display_title_exists = false;
        var idents = r.identifiers;
        if (idents?.Any() is true)
        {
            string? second_language = "";
            foreach (var dline in idents)
            {
                string? item_code = dline.item_code;
                switch (item_code)
                {
                    case "A.1":
                    {
                        // 'member state concerned'
                        // used here to estimate any non English title text listed
                        // (GetLanguage... function beneath main ProcessData function).

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            string? member_state = values[0].value;
                            if (!string.IsNullOrEmpty(member_state))
                            {
                                second_language = member_state.GetLanguageFromMemberState();
                            }
                        }
                        break;
                    }

                    case "A.3":
                    {
                        // Study scientific titles - may be multiple titles (but may just repeat).
                        // The first title is in English, any second in the country's own language/

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            int value_num = 0;
                            foreach (item_value v in values)
                            {
                                string? title = v.value;
                                value_num++;
                                if (title is not null && title.Length >= 4)
                                {
                                    if (title.AppearsGenuineTitle())
                                    {
                                        title = title.Trim().ReplaceApos();
                                        if (!title!.NameAlreadyPresent(titles))
                                        {
                                            string? lang_code = value_num == 1 ? "en" : second_language;
                                            int lang_use_id = value_num == 1 ? 11 : 22;
                                            titles.Add(new StudyTitle(sid, title, 16, "Registry scientific title",
                                                lang_code, lang_use_id, false, "From the EU CTR"));
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }

                    case "A.3.1":
                    {
                        // Study public titles - may be multiple titles (but may just repeat).
                        // The first title is in English, any second in the country's own language.

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            int value_num = 0;
                            foreach (item_value v in values)
                            {
                                string? title = v.value;
                                value_num++;
                                if (title is not null && title.Length >= 4)
                                {
                                    if (title.AppearsGenuineTitle())
                                    {
                                        title = title.Trim().ReplaceApos()!;
                                        if (! title.NameAlreadyPresent(titles))
                                        {
                                            string? lang_code = value_num == 1 ? "en" : second_language;
                                            int lang_use_id = value_num == 1 ? 11 : 22;
                                            titles.Add(new StudyTitle(sid, title, 15, "Registry public title",
                                                lang_code, lang_use_id, value_num == 1, "From the EU CTR"));
                                            if (value_num == 1)
                                            {
                                                // A default value has been stored and this will signify the 
                                                // display title.
                                                s.display_title = title;
                                                display_title_exists = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }

                    case "A.3.2":
                    {
                        // Study Acronym(s) - may be multiple (but may just repeat).
                        // The first acronym uses English, the second the country's own language.

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            foreach (item_value v in values)
                            {
                                string? acronym = v.value;
                                if (!string.IsNullOrEmpty(acronym))
                                {
                                    string acro_lc = acronym.Trim().ToLower();
                                    if (!acro_lc.StartsWith("not ") && !acro_lc.StartsWith("non ") && acro_lc != "none"
                                        && acro_lc.Length > 2 && acro_lc != "n/a" && acro_lc != "n.a." 
                                        && !acro_lc.StartsWith("no ap") && !acro_lc.StartsWith("no av"))
                                    {
                                        if (acro_lc.EndsWith(" trial"))
                                        {
                                            acronym = acronym[..acro_lc.LastIndexOf(" trial", StringComparison.Ordinal)];
                                        }
                                        if (acro_lc.EndsWith(" study"))
                                        {
                                            acronym = acronym[..acro_lc.LastIndexOf(" study", StringComparison.Ordinal)];
                                        }
                                        if (acro_lc.StartsWith("the "))
                                        {
                                            acronym = acronym[4..];
                                        }
                                        acronym = acronym.ReplaceApos();
                                        
                                        if (!acronym!.NameAlreadyPresent(titles) && acronym!.Length < 18)
                                        {
                                            titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation",
                                                false, "From the EU CTR"));
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }

                    case "A.5.1":
                    {
                        // identifier: ISRCTN (International Standard Randomised Controlled Trial)
                        // Number, if one present.

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            string? isrctn_id = values[0].value;
                            if (isrctn_id is not null && isrctn_id.ToLower().StartsWith("isrctn"))
                            {
                                identifiers.Add(new StudyIdentifier(sid, isrctn_id, 11, "Trial Registry ID",
                                    100126, "ISRCTN", null, null));
                            }
                        }
                        break;
                    }

                    case "A.5.2":
                    {
                        // identifier: NCT Number if one present.

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            string? nct_id = values[0].value;
                            if (nct_id is not null && nct_id.ToLower().StartsWith("nct"))
                            {
                                identifiers.Add(new StudyIdentifier(sid, nct_id, 11, "Trial Registry ID",
                                    100120, "ClinicalTrials.gov", null, null));
                            }
                        }
                        break;
                    }

                    case "A.5.3":
                    {
                        // identifier: WHO UTN Number, if one present.
                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            string? who_id = values[0].value;
                            if (who_id is not null && who_id.ToLower().StartsWith("u1111"))
                            {
                                identifiers.Add(new StudyIdentifier(sid, who_id, 11, "Trial Registry ID",
                                    100115, "International Clinical Trials Registry Platform", null, null));
                            }
                        }
                        break;
                    }
                }
            }
        }

        // Tidy up title data.
        // First ensure that there is a default and display title.

        if (!display_title_exists)
        {
            // No public title has been stored, so
            // use the registry title - should always be one.

            foreach (var t in titles)
            {
                if (t.title_type_id == 16)
                {
                    t.is_default = true;
                    s.display_title = t.title_text;
                    display_title_exists = true;
                    break;
                }
            }
        }

        if (!display_title_exists)
        {
            // Unlikely, but just in case - use an acronym.

            foreach (var t in titles)
            {
                if (t.title_type_id == 14)
                {
                    t.is_default = true;
                    s.display_title = t.title_text;
                    display_title_exists = true;
                    break;
                }
            }
        }

        // Add in an explanatory message... if still no title (!) -
        // there are a few early trials in EUCTR where this is the case

        if (!display_title_exists)
        {
            s.display_title = sid + " (No meaningful title provided)";
        }

        // Finally, truncate display_title if too long - some 
        // titles are extremely long...

        if (s.display_title!.Length > 400)
        {
            s.display_title = s.display_title[..400] + "...";
        }


        // Study design info

        string? study_description = null;
        int study_iec_type = 0;
        
        var feats = r.features;
        if (feats?.Any() is true)
        {
            foreach (var dline in feats)
            {
                string? item_code = dline.item_code;
                if (item_code is not null)
                {
                    if (item_code == "E.1.1")
                    {
                        // Condition(s) under study.

                        var values = dline.item_values;
                        if (values?.Any() is true)
                        {
                            foreach (var item_value in values)
                            {
                                string? name = item_value.value;
                                if (!string.IsNullOrEmpty(name) && !name.Contains('\r') && !name.Contains('\n') &&
                                    name.Length < 100)
                                {
                                    conditions.Add(new StudyCondition(sid, name, null, null));
                                }
                            }
                        }
                    }

                    if (item_code == "E.2.1" || item_code == "E.5.1")
                    {
                        // E.2.1 = primary objectives (may be multiple, i.e. in 2 languages, but may just repeat)
                        // case "E.5.1" = primary end points.
                        // Both are used to construct a study description.

                        if (item_code == "E.2.1")
                        {
                            var values = dline.item_values;
                            if (values?.Any() is true)
                            {
                                int indiv_value_num = 0;
                                string? study_objectives = null;
                                foreach (var v in values)
                                {
                                    indiv_value_num++;
                                    string? primary_obs = v.value?.StringClean();

                                    if (primary_obs is not null && primary_obs.Length >= 16 &&
                                        !primary_obs.ToLower().StartsWith("see ") &&
                                        !primary_obs.ToLower().StartsWith("not "))
                                    {
                                        if (indiv_value_num == 1)
                                        {
                                            study_objectives = !primary_obs.ToLower().StartsWith("primary")
                                                ? "Primary objectives: " + primary_obs
                                                : primary_obs;
                                        }
                                        else
                                        {
                                            study_objectives +=
                                                "\n(" + primary_obs + ")"; // Usually non English version.
                                        }
                                    }
                                }

                                // Start the study description with study objectives, if they are present.

                                if (study_objectives is not null)
                                {
                                    study_description = study_objectives;
                                }
                            }
                        }

                        if (item_code == "E.5.1")
                        {
                            var values = dline.item_values;
                            if (values?.Any() is true)
                            {
                                int indiv_value_num = 0;
                                string? study_endpoints = null;
                                foreach (var v in values)
                                {
                                    indiv_value_num++;
                                    string? end_points = v.value?.StringClean();

                                    if (end_points is not null && end_points.Length >= 16 &&
                                        !end_points.ToLower().StartsWith("see ") &&
                                        !end_points.ToLower().StartsWith("not "))
                                    {
                                        if (indiv_value_num == 1)
                                        {
                                            study_endpoints = !end_points.ToLower().StartsWith("primary")
                                                ? "Primary endpoints: " + end_points
                                                : end_points;
                                        }
                                        else
                                        {
                                            study_endpoints += "\n(" + end_points + ")";
                                        }
                                    }
                                }

                                // Continue the study description with study endpoints, if they are present.

                                if (study_endpoints is not null)
                                {
                                    study_description += string.IsNullOrEmpty(study_description)
                                        ? study_endpoints
                                        : "\n" + study_endpoints;
                                }
                            }
                        }
                    }

                    
                    if (item_code.Contains("E.7") || item_code.Contains("E.8"))
                    {
                        Tuple<int, string, int, string> new_feature = item_code switch
                        {
                            "E.7.1" => new Tuple<int, string, int, string>(20, "phase", 110, "Phase 1"),
                            "E.7.2" => new Tuple<int, string, int, string>(20, "phase", 120, "Phase 2"),
                            "E.7.3" => new Tuple<int, string, int, string>(20, "phase", 130, "Phase 3"),
                            "E.7.4" => new Tuple<int, string, int, string>(20, "phase", 135, "Phase 4"),
                            "E.8.1.1" => new Tuple<int, string, int, string>(22, "allocation type", 205, "Randomised"),
                            "E.8.1.2" => new Tuple<int, string, int, string>(24, "masking", 500, "None (Open Label)"),
                            "E.8.1.3" => new Tuple<int, string, int, string>(24, "masking", 505, "Single"),
                            "E.8.1.4" => new Tuple<int, string, int, string>(24, "masking", 510, "Double"),
                            "E.8.1.5" => new Tuple<int, string, int, string>(23, "intervention model", 305,
                                "Parallel assignment"),
                            "E.8.1.6" => new Tuple<int, string, int, string>(23, "intervention model", 310,
                                "Crossover assignment"),
                            _ => new Tuple<int, string, int, string>(0, "", 0, ""),
                        };

                        if (new_feature.Item1 != 0)
                        {
                            features.Add(new StudyFeature(sid, new_feature.Item1, new_feature.Item2,
                                new_feature.Item3, new_feature.Item4));
                        }
                    }
                }
            }
        }
        
        // Inclusion / exclusion criteria
        
        int num_inc_criteria = 0;
        string? ic = r.inclusion_criteria;
        if (!string.IsNullOrEmpty(ic))
        {
            ic.RegulariseStringEndings();
            List<Criterion>? crits = iech.GetNumberedCriteria(sid, ic, "inclusion");
            if (crits is not null)
            {
                int seq_num = 0;
                foreach (Criterion cr in crits)
                {    
                    seq_num++;
                    iec.Add(new StudyIEC(sid, seq_num, cr.Leader, cr.IndentLevel, 
                        cr.LevelSeqNum, cr.CritTypeId, cr.CritType, cr.CritText));
                }
                study_iec_type = (crits.Count == 1) ? 2 : 4;
                num_inc_criteria = crits.Count;
            }
        }

        string? ec = r.exclusion_criteria;
        if (!string.IsNullOrEmpty(ec))
        {
            ec.RegulariseStringEndings();
            List<Criterion>? crits = iech.GetNumberedCriteria(sid, ec, "exclusion");
            if (crits is not null)
            {
                int seq_num = num_inc_criteria;
                foreach (Criterion cr in crits)
                {
                    seq_num++;
                    iec.Add(new StudyIEC(sid, seq_num, cr.Leader, cr.IndentLevel, 
                        cr.LevelSeqNum, cr.CritTypeId, cr.CritType, cr.CritText));
                }
                study_iec_type += (crits.Count == 1) ? 5 : 6;
            }
        }
               
        s.iec_level = study_iec_type;

        // eligibility

        var population = r.population;
        if (population?.Any() is true)
        {
            var pgroups = new Dictionary<string, bool>
            {
                { "includes_under18", false },
                { "includes_in_utero", false },
                { "includes_preterm", false },
                { "includes_newborns", false },
                { "includes_infants", false },
                { "includes_children", false },
                { "includes_ados", false },
                { "includes_adults", false },
                { "includes_elderly", false },
                { "includes_women", false },
                { "includes_men", false },
            };

            foreach (var d_line in population)
            {
                // Each line indicates which of the age / gender groups should be set as true.

                string? item_code = d_line.item_code;
                string group_type = item_code switch
                {
                    "F.1.1" => "includes_under18",
                    "F.1.1.1" => "includes_in_utero",
                    "F.1.1.2" => "includes_preterm",
                    "F.1.1.3" => "includes_newborns",
                    "F.1.1.4" => "includes_infants",
                    "F.1.1.5" => "includes_children",
                    "F.1.1.6" => "includes_ados",
                    "F.1.2" => "includes_adults",
                    "F.1.3" => "includes_elderly",
                    "F.2.1" => "includes_women",
                    "F.2.2" => "includes_men",
                    _ => ""
                };

                if (group_type != "")
                {
                    pgroups[group_type] = true;
                }
            }

            // get gender eligibility information

            if (pgroups["includes_men"] && pgroups["includes_women"])
            {
                s.study_gender_elig = "All";
                s.study_gender_elig_id = 900;
            }
            else if (pgroups["includes_women"])
            {
                s.study_gender_elig = "Female";
                s.study_gender_elig_id = 905;
            }
            else if (pgroups["includes_men"])
            {
                s.study_gender_elig = "Male";
                s.study_gender_elig_id = 910;
            }
            else
            {
                s.study_gender_elig = "Not provided";
                s.study_gender_elig_id = 915;
            }

            // Try to establish age limits

            if (!pgroups["includes_under18"])
            {
                // No children or adolescents included.
                // If 'elderly' are included no age maximum is presumed.

                if (pgroups["includes_adults"] && pgroups["includes_elderly"])
                {
                    s.min_age = 18;
                    s.min_age_units = "Years";
                    s.min_age_units_id = 17;
                }
                else if (pgroups["includes_adults"])
                {
                    s.min_age = 18;
                    s.min_age_units = "Years";
                    s.min_age_units_id = 17;
                    s.max_age = 64;
                    s.max_age_units = "Years";
                    s.max_age_units_id = 17;
                }
                else if (pgroups["includes_elderly"])
                {
                    s.min_age = 65;
                    s.min_age_units = "Years";
                    s.min_age_units_id = 17;
                }
            }
            else
            {
                // Some under 18s included
                // First identify the situation where under-18s, adults and elderly are all included
                // corresponds to no age restrictions

                if (pgroups["includes_under18"] && pgroups["includes_adults"] && pgroups["includes_elderly"])
                {
                    // Leave min and max ages blank
                }
                else
                {
                    // First try and obtain a minimum age.
                    // Start with the youngest included and work up.

                    if (pgroups["includes_in_utero"] || pgroups["includes_preterm"] || pgroups["includes_newborns"])
                    {
                        s.min_age = 0;
                        s.min_age_units = "Days";
                        s.min_age_units_id = 14;
                    }
                    else if (pgroups["includes_infants"])
                    {
                        s.min_age = 28;
                        s.min_age_units = "Days";
                        s.min_age_units_id = 14;
                    }
                    else if (pgroups["includes_children"])
                    {
                        s.min_age = 2;
                        s.min_age_units = "Years";
                        s.min_age_units_id = 17;
                    }
                    else if (pgroups["includes_ados"])
                    {
                        s.min_age = 12;
                        s.min_age_units = "Years";
                        s.min_age_units_id = 17;
                    }

                    // Then try and obtain a maximum age.
                    // Start with the oldest included and work down.

                    if (pgroups["includes_adults"])
                    {
                        s.max_age = 64;
                        s.max_age_units = "Years";
                        s.max_age_units_id = 17;
                    }
                    else if (pgroups["includes_ados"])
                    {
                        s.max_age = 17;
                        s.max_age_units = "Years";
                        s.max_age_units_id = 17;
                    }
                    else if (pgroups["includes_children"])
                    {
                        s.max_age = 11;
                        s.max_age_units = "Years";
                        s.max_age_units_id = 17;
                    }
                    else if (pgroups["includes_infants"])
                    {
                        s.max_age = 23;
                        s.max_age_units = "Months";
                        s.max_age_units_id = 16;
                    }
                    else if (pgroups["includes_newborns"])
                    {
                        s.max_age = 27;
                        s.max_age_units = "Days";
                        s.max_age_units_id = 14;
                    }
                    else if (pgroups["includes_in_utero"] || pgroups["includes_preterm"])
                    {
                        s.max_age = 0;
                        s.max_age_units = "Days";
                        s.max_age_units_id = 14;
                    }
                }
            }
        }


        // topics (mostly IMPs)

        List<IMP> imp_list = new();
        var imps = r.imps;
        if (imps?.Any() is true)
        {
            int current_num = 0;
            IMP? imp = null;
            
            foreach (var impLine in imps)
            {
                int imp_num = impLine.imp_number;

                if (imp_num > current_num)
                {
                    // new imp class required to hold the values found below
                    // store any old one that exists first

                    if (imp is not null)
                    {
                        imp_list.Add(imp);
                    }
                    current_num = imp_num;
                    imp = new IMP(current_num);
                }

                string? item_code = impLine.item_code;
                if (item_code is not null && imp is not null)
                {
                    switch (item_code)
                    {
                        case "D.2.1.1.1":
                        {
                            // Trade name
                            var values = impLine.item_values;
                            if (values?.Any() is true)
                            {
                                string? topic_name = values[0].value;
                                string? name = topic_name?.ToLower();
                                if (!string.IsNullOrEmpty(name) && name != "not available"
                                     && name != "n/a" && name != "na" &&
                                     name != "not yet established" && name != "not yet extablished")
                                {
                                    imp.trade_name = topic_name!.Replace(((char)174).ToString(), ""); // drop reg mark
                                }
                            }

                            break;
                        }
                        case "D.3.1":
                        {
                            // Product name
                            var values = impLine.item_values;
                            if (values?.Any() is true)
                            {
                                string? topic_name = values[0].value;
                                string? name = topic_name?.ToLower();
                                if (!string.IsNullOrEmpty(name) && name != "not available"
                                     && name != "n/a" && name != "na" &&
                                     name != "not yet established" && name != "not yet extablished")
                                {
                                    imp.product_name = topic_name!.Replace(((char)174).ToString(), ""); // drop reg mark
                                }
                            }

                            break;
                        }
                        case "D.3.8":
                        {
                            // INN
                            var values = impLine.item_values;
                            if (values?.Any() is true)
                            {
                                string? topic_name = values[0].value;
                                string? name = topic_name?.ToLower();
                                if (!string.IsNullOrEmpty(name) && name != "not available"
                                     && name != "n/a" && name != "na" &&
                                     name != "not yet established" && name != "not yet extablished")
                                {
                                    imp.inn = topic_name!;
                                }
                            }
                            break;
                        }
                    }
                }
            }

            // Add the last one (if there have been any). 
            if (imp is not null)
            {
                imp_list.Add(imp);
            }

            // Process the imp list to get the best topic name' from those available
            // use the product name, or the INN, or the trade name, in that order,
            // and add the IMP as a topic, type 'chemical / agent'.

            if (imp_list.Count > 0)
            {

                foreach (IMP i in imp_list)
                {
                    string imp_name = "";
                    if (i.product_name is not null)
                    {
                        imp_name = i.product_name;
                    }
                    else if (i.inn is not null)
                    {
                        imp_name = i.inn;
                    }
                    else if (i.trade_name is not null)
                    {
                        imp_name = i.trade_name;
                    }

                    if (imp_name != "" && !imp_name.IMPAlreadyThere(topics))
                    {
                        topics.Add(new StudyTopic(sid, 12, "chemical / agent", imp_name));
                    }
                }
            }
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
                    conditions.Add(new StudyCondition(sid, t.term, 16, t.code));
                }
            }
        }

        
        var cs = r.countries;
        if (cs?.Any() is true)
        {
            foreach (var cline in cs)
            {
                string? country_name = cline.name?.Trim().ReplaceApos();
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
                        int? status_id = string.IsNullOrEmpty(country_status) ? null : country_status.GetStatusId();
                        countries.Add(new StudyCountry(sid, country_name, status_id, country_status));
                    }
                }
            }
        }


        // DATA OBJECTS and their attributes

        // ----------------------------------------------------------
        // initial data object is the EUCTR registry entry
        // ----------------------------------------------------------

        string object_title = "EU CTR registry entry";
        string object_display_title = s.display_title + " :: EU CTR registry entry";
        SplitDate? entered_in_db = null;
        if (r.entered_in_db is not null)
        {
            entered_in_db = r.entered_in_db.GetDatePartsFromISOString();
        }

        int? registry_pub_year = (entered_in_db is not null) ? entered_in_db.year : s.study_start_year;

        // create hash Id for the data object
        string sd_oid = sid + " :: 13 :: " + object_title;

        data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, registry_pub_year,
            23, "Text", 13, "Trial Registry entry", 100123, "EU Clinical Trials Register",
            12, download_datetime));

        // data object title is the single display title...
        object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
            22, "Study short name :: object type", true));

        // date of registry entry
        if (entered_in_db != null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                entered_in_db.year, entered_in_db.month, entered_in_db.day, entered_in_db.date_string));
        }

        // instance url 
        string details_url = r.details_url!; // cannot be null, else there would be no data!
        object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
            details_url, true, 35, "Web text"));

        // ----------------------------------------------------------
        // if there is a results url, add that in as well
        // ----------------------------------------------------------

        string? results_url = r.results_url;
        if (!string.IsNullOrEmpty(results_url))
        {
            object_title = "EU CTR results entry";
            object_display_title = s.display_title + " :: EU CTR results entry";
            sd_oid = sid + " :: 28 :: " + object_title;

            // get the date data if available

            string? results_first_date = r.results_first_date;
            string? results_revision_date = r.results_revision_date;
            SplitDate? results_date = null;
            SplitDate? results_revision = null;
            int? results_pub_year = null;
            if (!string.IsNullOrEmpty(results_first_date))
            {
                results_date = results_first_date.GetDatePartsFromEUCTRString();
                results_pub_year = results_date?.year;
            }

            if (!string.IsNullOrEmpty(results_revision_date))
            {
                results_revision = results_revision_date.GetDatePartsFromEUCTRString();
            }

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_pub_year,
                23, "Text", 28, "Trial registry results summary", 100123,
                "EU Clinical Trials Register", 12, download_datetime));

            // data object title is the single display title...
            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                22, "Study short name :: object type", true));

            // instance url 
            object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                results_url, true, 35, "Web text"));

            // dates
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

            // if there is a reference to a CSR pdf to download...
            // Seems to be on the web pages in two forms

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

                    // data object title is the single display title...
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

                // data object title is the single display title...
                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                    title_type_id, title_type, true));

                // instance url 
                object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                    results_pdf_link, true, 11, "PDF"));

            }
        }


        // Eit contributors - try to ensure properly categorised.
        // All contributors originally down as organisations
        // Try and see if some are actually people
        
        List<StudyOrganisation> orgs2 = new();
        if (organisations.Count > 0)
        {
            foreach (StudyOrganisation g in organisations)
            { 
                bool add = true;
                string? orgname = g.organisation_name?.ToLower();
                if (orgname is not null && orgname.IsAnIndividual())
                {
                    string? person_full_name = g.organisation_name.TidyPersonName();
                    if (person_full_name is not null)
                    {
                        people.Add(new StudyPerson(sid, g.contrib_type_id, g.contrib_type, person_full_name,
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


        s.brief_description = study_description;

        s.identifiers = identifiers;
        s.titles = titles;
        s.organisations = orgs2;
        s.people = people;
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






