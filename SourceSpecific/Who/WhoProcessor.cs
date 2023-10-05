using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Who;

public class WHOProcessor : IStudyProcessor
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

        Who_Record? r = null;
        try
        {
           r = JsonSerializer.Deserialize<Who_Record?>(json_string, json_options);
        }
        catch (Exception ex)
        {
            string e = ex.Message;
        }

        if (r is null)
        {
            _logging_helper.LogError($"Unable to deserialise json file to Who_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }

        Study s = new();

        // get date retrieved in object fetch
        // transfer to study and data object records

        List<StudyIdentifier> identifiers = new();
        List<StudyTitle> titles = new();
        List<StudyFeature> features = new();
        List<StudyOrganisation> organisations = new();
        List<StudyPerson> people = new();
        List<StudyCountry> countries = new();
        List<StudyCondition> conditions = new();
        List<StudyIEC> iec = new();

        List<DataObject> data_objects = new();
        List<ObjectTitle> data_object_titles = new();
        List<ObjectDate> data_object_dates = new();
        List<ObjectInstance> data_object_instances = new();

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Basics - id, source, registration
        ///////////////////////////////////////////////////////////////////////////////////////
        
        string? sid = r.sd_sid;

        if (string.IsNullOrEmpty(sid))
        {
            _logging_helper.LogError($"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        int? source_id = r.source_id;
        string? source_name = source_id.GetSourceName();

        // Do initial identifier representing the registry id.

        SplitDate? registration_date = null;
        if (!string.IsNullOrEmpty(r.date_registration))
        {
            registration_date = r.date_registration.GetDatePartsFromISOString();
        }

        identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", source_id,
                                    source_name, registration_date?.date_string, null));

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study titles
        ///////////////////////////////////////////////////////////////////////////////////////

        string? public_title = r.public_title;
        bool public_title_present = public_title.IsNotPlaceHolder();
        if (public_title_present)
        {
            public_title = public_title.LineClean();
        }

        string? scientific_title = r.scientific_title;
        bool scientific_title_present = scientific_title.IsNotPlaceHolder();
        if (scientific_title_present)
        {
            scientific_title = scientific_title.LineClean();
        }
        string source_string = "From the " + source_name;


        if (!public_title_present)
        {
            // No public title - use scientific title as default.

            if (scientific_title_present)
            {
                titles.Add(scientific_title!.Length < 11
                    ? new StudyTitle(sid, scientific_title, 14, "Acronym or Abbreviation", true, source_string)
                    : new StudyTitle(sid, scientific_title, 16, "Registry scientific title", true, source_string));

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

            titles.Add(public_title!.Length < 11
                ? new StudyTitle(sid, public_title, 14, "Acronym or Abbreviation", true, source_string)
                : new StudyTitle(sid, public_title, 15, "Registry public title", true, source_string));

            if (scientific_title_present && 
                    !String.Equals(scientific_title!, public_title, StringComparison.CurrentCultureIgnoreCase))
            {
                titles.Add(new StudyTitle(sid, scientific_title, 16, "Registry scientific title", false, source_string));
            }

            s.display_title = public_title;
        }

        // English used as the default title language.
        // Need a mechanism, here to try and identify at least major language variations
        // e.g. Spanish, German, French - may be linkable to the source registry

        s.title_lang_code = "en";  
        

        ///////////////////////////////////////////////////////////////////////////////////////
        // Study description
        ///////////////////////////////////////////////////////////////////////////////////////

        string? interventions = r.interventions;
        if (!string.IsNullOrEmpty(interventions))
        {
            if (!interventions.ToLower().Contains("not applicable") && !interventions.ToLower().Contains("not selected")
                && interventions.ToLower() != "n/a" && interventions.ToLower() != "na")
            {
                interventions = interventions.FullClean();
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
                && primary_outcome.ToLower() != "n/a" && primary_outcome.ToLower() != "na")
            {
                primary_outcome = primary_outcome.FullClean();
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
                && design_string.ToLower() != "n/a" && design_string.ToLower() != "na")
            {
                design_string = design_string.FullClean();
                if (!design_string!.ToLower().StartsWith("primary"))
                {
                    design_string = "Study Design: " + design_string;
                }
                s.brief_description += string.IsNullOrEmpty(s.brief_description) ? design_string : "\n" + design_string;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study data sharing statement
        ///////////////////////////////////////////////////////////////////////////////////////

        string? ipd_plan = r.ipd_plan;
        if (!string.IsNullOrEmpty(ipd_plan)
            && ipd_plan.Length > 10
            && ipd_plan.ToLower() != "not available"
            && ipd_plan.ToLower() != "not avavilable"
            && ipd_plan.ToLower() != "not applicable"
            && !ipd_plan.Contains("justification or reason for"))
        {
            ipd_plan = ipd_plan.FullClean();
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
            ipd_description = ipd_description.FullClean();
            s.data_sharing_statement += string.IsNullOrEmpty(s.data_sharing_statement) ? ipd_description : "\n" + ipd_description;
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study start date, enrolment, type, status, gender, age limits
        ///////////////////////////////////////////////////////////////////////////////////////

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
                s.study_type = study_type;
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


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study contributors  - contacts, sponsors, funders
        ///////////////////////////////////////////////////////////////////////////////////////

        string study_lead = "";
        string? s_givenname = r.scientific_contact_givenname;
        string? s_familyname = r.scientific_contact_familyname;
        string? s_affiliation = r.scientific_contact_affiliation;
        
        string? p_givenname = r.public_contact_givenname;
        string? p_familyname = r.public_contact_familyname;
        string? p_affiliation = r.public_contact_affiliation;
        
        if (!string.IsNullOrEmpty(s_givenname) || !string.IsNullOrEmpty(s_familyname))
        {
            string s_full_name = (s_givenname + " " + s_familyname).Trim();
            s_full_name = s_full_name.LineClean()!;

            if (s_full_name.AppearsGenuinePersonName())
            {
                s_full_name = s_full_name.TidyPersonName()!;
                study_lead = s_full_name;  // for later comparison
                if (!string.IsNullOrEmpty(s_full_name))
                {
                    s_affiliation = s_affiliation.TidyOrgName(sid).StandardisePharmaName();
                    string? affil_org = s_affiliation?.ExtractOrganisation(sid);
                    people.Add(new StudyPerson(sid, 51, "Study Lead", s_full_name, s_affiliation, null, affil_org));
                }
            }
        }
        
        if (!string.IsNullOrEmpty(p_givenname) || !string.IsNullOrEmpty(p_familyname))
        {
            string? p_full_name = (p_givenname + " " + p_familyname).Trim();
            p_full_name = p_full_name.LineClean();
            if (p_full_name.AppearsGenuinePersonName()) 
            {
                p_full_name = p_full_name.TidyPersonName();
                if (p_full_name != "" && p_full_name != study_lead)  // often duplicated
                {
                    p_affiliation = p_affiliation.TidyOrgName(sid).StandardisePharmaName();
                    string? affil_org = p_affiliation?.ExtractOrganisation(sid);
                    people.Add(new StudyPerson(sid, 56, "Public Contact", p_full_name, p_affiliation, null, affil_org));
                }

            }
        }

        string? sponsor_name = "No organisation name provided in source data";
        bool? sponsor_is_org = null;

        string? primary_sponsor = r.primary_sponsor;
        if (!string.IsNullOrEmpty(primary_sponsor))
        {
            sponsor_name = primary_sponsor.TidyOrgName(sid).StandardisePharmaName();
            if (sponsor_name.IsNotPlaceHolder())
            {
                if (!sponsor_name.AppearsGenuineOrgName())
                {
                    sponsor_is_org = false;
                    sponsor_name = sponsor_name.TidyPersonName();
                    if (sponsor_name == study_lead)  // May be the case if sponsor appears to be a person
                    {
                        // change study lead contribution to sponsor-investigator
                        if (people.Any())
                        {
                            foreach (StudyPerson sp in people)
                            {
                                if (sp.contrib_type_id == 51 && sp.person_full_name == sponsor_name)
                                {
                                    sp.contrib_type_id = 70;
                                    sp.contrib_type = "Sponsor-investigator";
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        people.Add(new StudyPerson(sid, 54, "Trial Sponsor", null, null, sponsor_name, null, null));
                    }
                }
                else
                {
                    sponsor_is_org = true;
                    organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", null, sponsor_name));
                }
            }
        }
        
        string? funders = r.source_support;
        if (!string.IsNullOrEmpty(funders))
        {
            string[] funder_names = funders.GetFunders(source_id);
           
            if (funder_names.Any())
            {
                foreach (string funder in funder_names)
                {
                    string? funder_name = funder.TidyOrgName(sid).StandardisePharmaName();
                    if (!string.IsNullOrEmpty(funder_name))
                    {
                        if (source_id is 100118 or 109108) // Chinese registry or ITMCTR 
                        {
                            // Check if one of the 'general' Chinese funding terms
                            // May produce an empty string or the word "sponsor"

                            funder_name = funder_name.CheckChineseFunderType();
                        }
                        if (source_id == 100122) // Cuban registry
                        {
                            if (funder_name.ToLower() is "government funds" or "government found")
                            {
                                funder_name = "Reported as government funded, no further details";
                            }
                        }
                        if (funder_name.IsNotPlaceHolder())
                        {
                            if (funder_name == "sponsor"
                                || funder_name.ToLower().Contains("bitte wenden sie sich an den sponsor")
                                || funder_name.ToLower().Contains("please refer to primary sponsor"))
                            {
                                // often from DRKS - implies sponsor also the funder
                                // the records will be combined later in the coding process

                                organisations.Add(new StudyOrganisation(sid, 58, "Study Funder", null,
                                    sponsor_name));
                            }
                            else
                            {
                                if (!funder_name.AppearsGenuineOrgName()) // Add funder as an individual
                                {
                                    funder_name = funder_name.TidyPersonName();
                                    people.Add(new StudyPerson(sid, 58, "Study Funder", null, null,
                                           funder_name, null, null));
                                }
                                else
                                {
                                    if (funder_name != "") // Add funder as an organisation. 
                                    {
                                        organisations.Add(new StudyOrganisation(sid, 58, "Study Funder", null,
                                            funder_name));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        

        ///////////////////////////////////////////////////////////////////////////////////////
        // Study features
        ///////////////////////////////////////////////////////////////////////////////////////

        var study_feats = r.study_features;
        if (study_feats?.Any() is true)
        {
            foreach(var f in study_feats)
            { 
                int? f_type_id = f.ftype_id;
                string? f_type = f.ftype;
                int? f_value_id = f.fvalue_id;
                string? f_value = f.fvalue;
                features.Add(new StudyFeature(sid, f_type_id, f_type, f_value_id, f_value));
            }
        }

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study identifiers
        ///////////////////////////////////////////////////////////////////////////////////////

        var sids = r.secondary_ids;
        if (sids?.Any() is true)
        {
            foreach (var id in sids)
            {
                int? sec_id_source = id.sec_id_source;
                string? processed_id = id.processed_id;

                if (sec_id_source is not null && id.sec_id_type is not null)   // Already identified in DL process
                {
                    source_name = sec_id_source.GetSourceName();
                    identifiers.Add(new StudyIdentifier(sid, processed_id, id.sec_id_type_id, id.sec_id_type, 
                                                        sec_id_source, source_name));
                }
                else if (sec_id_source is null && sponsor_name is not null && processed_id is not null)
                {
                    processed_id = processed_id.Trim('-', ':', ' ', '/', '*', '.');
                    if (processed_id.Length > 2)
                    {
                        string sponsor_name_lower = sponsor_name.ToLower();
                        if (source_id is 100116)        
                        {
                            identifiers.Add(sid.TryToGetANZIdentifier(processed_id, sponsor_is_org, sponsor_name));
                        }
                        else if (source_id is 100118)
                        {
                            StudyIdentifier? si =
                                sid.TryToGetChineseIdentifier(processed_id, sponsor_is_org, sponsor_name);
                            if (si is not null)
                            {
                                identifiers.Add(si);
                            }
                        }
                        else if (source_id == 100127)
                        {
                            identifiers.Add(sid.TryToGetJapaneseIdentifier(processed_id, sponsor_is_org,
                                sponsor_name));
                        }
                        else if (source_id == 100132)
                        {
                            List<string> possible_ids = new();
                            
                            // May be compound...
                            
                            if (processed_id.Contains("//"))
                            {
                                possible_ids = processed_id.SplitNTRIdString("//");
                            }
                            else if (processed_id.Contains("/ "))
                            {
                                possible_ids = processed_id.SplitNTRIdString("/ ");
                            }
                            else if (processed_id.Contains(" /"))
                            {
                                possible_ids = processed_id.SplitNTRIdString(" /");
                            }
                            else
                            {
                                possible_ids.Add(processed_id);
                            }
                            
                            foreach(string poss_id in possible_ids)
                            {
                                StudyIdentifier? si =
                                    sid.TryToGetDutchIdentifier(poss_id, sponsor_is_org, sponsor_name);
                                if (si is not null)
                                {
                                    identifiers.Add(si);
                                }
                            }
                        }
                        else if (sponsor_name_lower is "na" or "n/a" or "no" or "none" or "not available"
                                     or "no sponsor"
                                 || sponsor_name is "-" or "--")
                        {
                            identifiers.Add(new StudyIdentifier(sid, processed_id, 1, "Type not provided",
                                12, "No organisation name provided in source data"));
                        }
                        else if (sponsor_is_org is true)
                        {
                            // The default is therefore to assume any remaining secondary id will be a sponsor Id
                            // Normally the case but cannot be guaranteed!
                            
                            identifiers.Add(
                                new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", null, sponsor_name));
                        }
                        else
                        {
                            identifiers.Add(new StudyIdentifier(sid, processed_id, 14, "Sponsor ID", 12,
                                "No organisation name provided in source data"));
                        }
                    }
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study conditions
        ///////////////////////////////////////////////////////////////////////////////////////
       
        List<string>? condList = r.condition_list;
        if (condList?.Any() is true)
        {
            // If Indian CTR (source = 100121) multiple conditions and codes may be
            // in one string and therefore need splitting

            List<string> cList = source_id == 100121 ? condList.CTRIConditions() : condList;

            // Do any of the condition strings contain commas? If this is the case it might be multiple conditions
            // but could also just be a condition name with a qualifier
            // In the former case, if they are multiple codes, each part is usually the same length.
            // other multiples will need to be picked up by coding them as separate conditions

            List<string> cList2 = new();
            foreach (string cn in cList)
            {
                bool no_splitting_occured = true;
                string c2 = cn;

                if (c2.StartsWith("br />"))
                {
                    c2 = c2[5..].Trim(); // remove residual break lines (esp. common in Dutch entries)
                }

                if (c2.Contains("br />"))
                {
                    // Dutch entries often split the line into English and Dutch using this

                    string[] poss_list = c2.Split("br />", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (poss_list.Length > 1)
                    {
                        for (int i = 0; i < poss_list.Length; i++)
                        {
                            cList2.Add(poss_list[i]);
                        }
                        no_splitting_occured = false;
                    }
                }

                else if (c2.Contains(','))
                {
                    string[] poss_list = c2.Split(',', StringSplitOptions.TrimEntries);
                    bool add_to_list = true;
                    int test_length = poss_list[0].Length;
                    if (test_length <= 20)
                    {
                        for (int i = 1; i < poss_list.Length; i++)
                        {
                            if (poss_list[i].Length != test_length)
                            {
                                add_to_list = false;
                                break;
                            }
                        }
                        if (add_to_list)
                        {
                            for (int i = 0; i < poss_list.Length; i++)
                            {
                                cList2.Add(poss_list[i]);
                            }
                            no_splitting_occured = false;
                        }
                    }
                }

                if (no_splitting_occured)
                {
                    cList2.Add(c2);
                }
            }


            // second loop, after any further splits above

            string? code = null, code_system = null;  
            string? condition_term = null;

            foreach (string cn in cList2)
            {
                if (!string.IsNullOrEmpty(cn))
                {
                    char[] chars_to_trim = { ' ', '?', ':', '*', '/', '-', '_', '+', '=', '>', '<', '&', ',' };
                    string? cn_trimmed = cn.Trim(chars_to_trim);

                    if (!string.IsNullOrEmpty(cn_trimmed) &&
                        cn_trimmed.ToLower() != "not applicable" && cn_trimmed.ToLower() != "&quot" && cn_trimmed.ToLower() != "unspecified")
                    {
                        cn_trimmed = cn_trimmed.Replace(" - ", "-");  // close up around hyphens

                        if (Regex.Match(cn_trimmed, @"^\d\.").Success)  // remove digit-dot at beginning, used in some lists (esp.Dutch)
                        {
                            cn_trimmed = cn_trimmed[2..].Trim();
                        }
                                                
                        if(cn_trimmed.StartsWith('(') && cn_trimmed.EndsWith(')')) // Remove paired opening and closing brackets
                        {
                            cn_trimmed.Trim('(', ')');
                        }
                        
                        if (!Regex.Match(cn_trimmed[1..], @"[A-Za-z]").Success) // if all numbers bar first character close up any spaces
                        {
                            cn_trimmed = cn_trimmed[0] + cn_trimmed[1..].Replace(" ", "");
                        }

                        code = null; code_system = null; condition_term = null;  // re-initialise

                        if (cn_trimmed.Contains("***"))
                        {
                            // This initial test because conditions from the Indian trial registry may
                            // have already been split using *** as a separator between code and term

                            int star_pos = cn_trimmed.IndexOf("***");
                            code = cn_trimmed[..star_pos];
                            code_system = "ICD 10";
                            if (cn_trimmed.Length > star_pos + 3)
                            { 
                                 condition_term = cn_trimmed[(star_pos + 3)..];
                            }
                        }

                        else if (cn_trimmed.Length <= 10)
                        {
                            // Is the condition represented only by a code?
                            // Some common situations below (first ensure first letter is upper case)

                            cn_trimmed = cn_trimmed[0].ToString().ToUpper() + cn_trimmed[1..];

                            if (cn_trimmed.Length == 3)
                            {
                                if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}$").Success)
                                {
                                    code = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}$").Value;  // a single ID 10 code, like C01
                                    code_system = "ICD 10";
                                }
                            }

                            else if (cn_trimmed.Length == 4)
                            {
                                if (Regex.Match(cn_trimmed, @"^[A-Z]\d{3}$").Success)  // nornmally an ICD10 code without the digit
                                {
                                    code = cn_trimmed[..3] + "." + cn_trimmed[^1];
                                    code_system = "ICD 10";
                                }
                                else if (Regex.Match(cn_trimmed, @"^[A-Z0-9][A-Z][0-9][A-Z0-9]$").Success)  // might be an ID 11 code, like A0A1, EM0Z
                                {
                                    code = Regex.Match(cn_trimmed, @"^[A-Z0-9][A-Z][0-9][A-Z0-9]$").Value;  
                                    code_system = "ICD 11";
                                }
                            }

                            else if (cn_trimmed.Length == 5)
                            {
                                // might be an ICD sub-code, like A03.2  

                                if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d$").Success)
                                {
                                    code = cn_trimmed;  // a single ID 10 code, like C01
                                    code_system = "ICD 10";
                                }
                            }

                            else if (cn_trimmed.Length == 6)
                            {
                                // might be an ICD sub-code, like A03.23

                                if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}$").Success)
                                {
                                    code = cn_trimmed;
                                    code_system = "ICD 10";
                                }

                                // or may be a 'double' ICD range, e.g. 'A00B99'

                                if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}[A-Z]\d{2}$").Success)
                                {
                                    code = cn_trimmed[..3] + "-" + cn_trimmed[3..];
                                    code_system = "ICD 10";
                                }
                            }

                            else if (cn_trimmed.Length == 7)
                            {
                                // might be an 'C' or 'D' mesh code
                                if (Regex.Match(cn_trimmed, @"^C\d{6}$").Success || Regex.Match(cn_trimmed, @"^D\d{6}$").Success)
                                {
                                    code = cn_trimmed;
                                    code_system = "MeSH";
                                }
                                else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{3}$").Success)
                                {
                                    code = cn_trimmed;
                                    code_system = "MeSH Tree";
                                }
                                if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}-[A-Z]\d{2}$").Success)
                                {
                                    code = cn_trimmed;
                                    code_system = "ICD 10";
                                }
                            }

                            else if (cn_trimmed.Length == 10)
                            {
                                // might be an 'C' or 'D' mesh code
                                if (Regex.Match(cn_trimmed, @"^C\d{9}$").Success || Regex.Match(cn_trimmed, @"^D\d{9}$").Success)
                                {
                                    code = cn_trimmed;
                                    code_system = "MeSH";
                                }
                            }

                            if (code_system is null)
                            {
                                condition_term = cn_trimmed; // no code found, just add as a condition
                            }

                        }

                        else if (cn_trimmed.Contains("generalization"))
                        {
                            string code_string = "";

                            // Use regex to get the second, more general code

                            if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}-\[generalization [A-Z]\d{2}.\d:").Success)
                            {
                                code_string = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}-\[generalization [A-Z]\d{2}\.\d:").Value.Trim();
                                code = Regex.Match(code_string, @"[A-Z]\d{2}\.\d:").Value.Trim(':');
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}-\[generalization [A-Z]\d{2}:").Success)
                            {
                                code_string = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}-\[generalization [A-Z]\d{2}:").Value.Trim();
                                code = Regex.Match(code_string, @"[A-Z]\d{2}:").Value.Trim(':');
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d-\[generalization [A-Z]\d{2}").Success)
                            {
                                code_string = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d-\[generalization [A-Z]\d{2}:").Value.Trim();
                                code = Regex.Match(code_string, @"[A-Z]\d{2}:").Value.Trim(':');
                            }

                            if (code is not null)
                            {
                                code_system = "ICD 10";
                                condition_term = cn_trimmed.Substring(code_string.Length).Trim(']').Trim();
                            }
                            else
                            {
                                condition_term = cn_trimmed;
                            }
                        }

                        else if (cn_trimmed.ToLower().StartsWith("meddra"))
                        {
                            code_system = "MedDRA";
                            if (cn_trimmed.Contains(':'))
                            {
                                if (cn_trimmed.ToLower().StartsWith("meddra-llt"))
                                {
                                    cn_trimmed = cn_trimmed[10..];
                                }
                                else if (cn_trimmed.ToLower().StartsWith("meddra-"))
                                {
                                    cn_trimmed = cn_trimmed[7..];
                                }

                                string[] components = cn_trimmed.Split(':', StringSplitOptions.TrimEntries);

                                if (components.Length == 2)
                                {
                                    if (!Regex.Match(components[0], @"[A-Za-z]").Success)
                                    {
                                        code = components[0];
                                        condition_term = components[1];
                                    }
                                    else if (!Regex.Match(components[1], @"[A-Za-z]").Success)
                                    {
                                        code = components[1];
                                        condition_term = components[0];
                                    }
                                }
                            }

                            if (code is null)
                            {
                                condition_term = cn_trimmed; // no code found (or meddra code above need expanding), just add as a condition
                            }
                        }

                        else
                        {
                            // string may contain codes (usually ICD for WHO data) but these will normally be followed
                            // by text or - more commonly - the condition will just be expressed as text

                            if (Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}$").Success ||
                                Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}$").Success ||
                                Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}\.\d{3}$").Success ||
                                Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}\.\d{3}\.\d{3}$").Success)
                            {
                                code = cn_trimmed;
                                code_system = "MeSH Tree";
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}\.\d{3}").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}\.\d{3}").Value.Trim();
                                code_system = "MeSH Tree";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}\.\d{3}").Value.Trim();
                                code_system = "MeSH Tree";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                            }
                                                        
                            else if (Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Za-z]\d{2}\.\d{3}\.\d{3}").Value.Trim();
                                code_system = "MeSH Tree";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                            }
                     

                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}$").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{2}$").Value.Trim();
                                code_system = "ICD 10";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                            }


                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d-[A-Z]\d{2}\.\d?").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d-[A-Z]\d{2}\.\d?").Value.Trim();
                                code_system = "ICD 10";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}(\.\d)?\s?").Success)
                            {
                                // rarely, this is an abnormal MESH tree code that has 'slipped through'
                                // or a letter followed by a large string of numbers

                                if (!Regex.Match(cn_trimmed, @"^[A-Z]\d{2}\.\d{3}").Success &&
                                    !Regex.Match(cn_trimmed, @"^[A-Z]\d{4}").Success)
                                {
                                    code = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}(\.\d)?\s?").Value.Trim();
                                    code_system = "ICD 10";
                                    condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                                }
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{2}-[A-Z]\d{2}\s?").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Z]\d{2}-[A-Z]\d{2}\s?").Value.Trim();
                                code_system = "ICD 10";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                            }

                            else if (Regex.Match(cn_trimmed, @"^[A-Z]\d{3}\s?").Success)
                            {
                                code = Regex.Match(cn_trimmed, @"^[A-Z]\d{3}\s?").Value.Trim();
                                code_system = "ICD 10";
                                condition_term = cn_trimmed.Substring(code.Length).Trim(chars_to_trim);
                                code = code[..3] + "." + code[^1..];
                            }

                            if (code_system is null)
                            {
                                condition_term = cn_trimmed; // no code found, just add as a condition
                            }
                        }
                    }
                }

                // Check not duplicated before adding.

                bool add_condition = true;
                if (conditions.Count > 0)
                {
                    foreach (StudyCondition sc in conditions)
                    {
                        if (condition_term is not null)
                        { 
                            if (condition_term.ToLower() == sc.original_value?.ToLower())
                            {
                                add_condition = false;
                                break;
                            }
                        }

                        if (add_condition && code is not null)
                        {
                            if (code.ToLower() == sc.original_ct_code?.ToLower())
                            {
                                add_condition = false;
                                break;
                            }
                        }

                    }
                }

                if (add_condition)
                {
                    if (code is null)
                    {
                        conditions.Add(new StudyCondition(sid, condition_term));
                    }
                    else
                    {
                        if (code_system is not null)
                        {
                            int? code_system_type_id = code_system.GetCTTypeId();
                            conditions.Add(new StudyCondition(sid, condition_term, code_system_type_id, code_system, code));
                        }
                    }
                }

            }
        }
            
        
        if (conditions.Any())
        {
            conditions = conditions.RemoveNonInformativeConditions();
        }                    

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study inclusion / exclusion criteria
        ///////////////////////////////////////////////////////////////////////////////////////

        string? ic = r.inclusion_criteria;
        string? ec = r.exclusion_criteria;
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


        ///////////////////////////////////////////////////////////////////////////////////////
        // Registry entry data object
        ///////////////////////////////////////////////////////////////////////////////////////

        string? name_base = s.display_title;
        string? remote_url = "";
        string? reg_prefix = source_id.GetRegistryPrefix();
        if (reg_prefix is not null)
        {
            string object_title = reg_prefix + "registry web page";
            string object_display_title = name_base + " :: " + reg_prefix + "registry web page";
            string sd_oid = sid + " :: 13 :: " + object_title;

            int? pub_year = registration_date?.year;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, pub_year, 23, "Text", 13, "Trial Registry entry",
                source_id, source_name, 12, download_datetime));

            remote_url = r.remote_url;
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

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Results link data object
        ///////////////////////////////////////////////////////////////////////////////////////

        string? results_url_link = r.results_url_link;
        string? results_url_protocol = r.results_url_protocol;
        string? results_date_posted = r.results_date_posted;
        string? results_date_completed = r.results_date_completed;

        SplitDate? results_posted_date = results_date_posted?.GetDatePartsFromISOString();
        SplitDate? results_completed_date = results_date_completed?.GetDatePartsFromISOString();

        if (!string.IsNullOrEmpty(results_url_link))
        {
            string results_link = results_url_link.ToLower();
            if (results_link.Contains("http") 
                && results_link != remote_url   // Exclude those that refer to the registry page
                && !results_link.Contains("clinicaltrials.gov")  // Exclude those on CTG - should be picked up there
                && !results_link.Contains("sharing-accessing-data/contributing-data"))   // generic WWWarn data page)
            {

                if (source_id == 100124 && r.results_date_posted is null)
                {
                    // a place-holder - ignore
                }
                else
                {
                    string object_title = "Results summary";
                    string object_display_title = name_base + " :: " + "Results summary";
                    string sd_oid = sid + " :: 28 :: " + object_title;

                    int? results_pub_year = results_posted_date?.year;

                    // In practice may not be in the registry

                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_pub_year,
                                        23, "Text", 28, "Trial registry results summary",
                                        source_id, source_name, 12, download_datetime));

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
        }

        if (!string.IsNullOrEmpty(results_url_protocol))
        {
            string prot_url = results_url_protocol.ToLower();
            if (prot_url.Contains("http")
                && source_id != 100124          // ignore the DRKS urls, which all seem to be place-holders at the moment (on the registry web page)
                && results_url_protocol != remote_url          // avoid duplicate references
                && results_url_protocol != results_url_link
                && !prot_url.Contains("clinicaltrials.gov")   // CTG links should be picked up elsewhere
                && !prot_url.Contains("sharing-accessing-data/contributing-data"))   // generic WWWarn data page
            {
                // presumed to be a download or a web reference
                
                string resource_type;
                int resource_type_id;
                string url_link;
                int url_start = prot_url.IndexOf("http", StringComparison.Ordinal);

                if (results_url_protocol.Contains(".pdf"))
                {
                    resource_type = "PDF";
                    resource_type_id = 11;
                    int pdf_end = prot_url.IndexOf(".pdf", StringComparison.Ordinal) + 4;
                    url_link = results_url_protocol[url_start..pdf_end];

                }
                else if (prot_url.Contains(".doc"))
                {
                    resource_type = "Word doc";
                    resource_type_id = 16;
                    if (prot_url.Contains(".docx"))
                    {
                        int docx_end = prot_url.IndexOf(".docx", StringComparison.Ordinal) + 5;
                        url_link = results_url_protocol[url_start..docx_end];
                    }
                    else
                    {
                        int doc_end = prot_url.IndexOf(".doc", StringComparison.Ordinal) + 4;
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


                int object_type_id; string object_type;
                if (prot_url.ToLower().Contains("prot"))
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

                // almost certainly not in or managed by the registry - may rarely be identifiable from the url
                
                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_pub_year, 
                    23, "Text", object_type_id, object_type,
                    null, null, 11, download_datetime));

                data_object_instances.Add(new ObjectInstance(sd_oid, null, null,
                                    url_link, true, resource_type_id, resource_type));
            }
        }

        
        ///////////////////////////////////////////////////////////////////////////////////////
        // Study countries
        ///////////////////////////////////////////////////////////////////////////////////////
 
        var countryList = r.country_list;
        if (countryList?.Any() is true)
        {
            foreach (var country in countryList)
            {
                if (!string.IsNullOrEmpty(country))
                {
                    string country_name = country;
                    country_name = country_name.LineClean()!;

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

                    string c_lower = country_name.ToLower();
                    if (c_lower.Length > 1 && c_lower != "na"
                                           && c_lower != "n a" && c_lower != "other" && c_lower != "nothing"
                                           && c_lower != "not applicable" && c_lower != "not provided"
                                           && c_lower != "etc" && c_lower != "Under selecting")
                    {
                        if (c_lower != "none" && c_lower != "nnone"
                                              && c_lower != "mone" && c_lower != "none."
                                              && c_lower != "non" && c_lower != "noe"
                                              && c_lower != "no country" && c_lower != "many"
                                              && c_lower != "north" && c_lower != "south")
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
                                foreach (var t in country_list)
                                {
                                    string ci = t.Trim();
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
        else
        {
             // Empty country list. If an NL study include the Netherlands. (The Dutch registry data
             // does not include any countries but this seems a reasonable assumption!)
             
             if (sid.StartsWith("NL"))
             {
                 countries.Add(new StudyCountry(sid, "Netherlands"));
             }
        }

        // Check contributors - try to ensure properly categorised
        // check if a group inserted as an individual, and then
        // check if an individual added as a group.
        
        List<StudyPerson> people2 = new();
        if (people.Count > 0)
        {
            foreach (StudyPerson p in people)
            {
                bool add = true;
                string? full_name = p.person_full_name?.ToLower();
                if (!full_name.AppearsGenuinePersonName())
                {
                    add = false;
                    string? organisation_name = p.person_full_name.TidyOrgName(sid);
                    if (organisation_name.AppearsGenuineOrgName())
                    {
                        organisations.Add(new StudyOrganisation(sid, p.contrib_type_id, p.contrib_type,
                            null, organisation_name));
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
                if (!org_name.AppearsGenuineOrgName())
                {
                    add = false;
                    string? person_full_name = g.organisation_name.TidyPersonName();
                    if (person_full_name.AppearsGenuinePersonName())
                    {
                        people2.Add(new StudyPerson(sid, g.contrib_type_id, g.contrib_type, person_full_name,
                            null, null, null));
                    }
                }
                if (add)
                {
                    orgs2.Add(g);
                }
            }
        }
        

        ///////////////////////////////////////////////////////////////////////////////////////
        // Construct final study object
        ///////////////////////////////////////////////////////////////////////////////////////
        
        s.identifiers = identifiers;
        s.titles = titles;
        s.features = features;
        s.people = people2;
        s.organisations = orgs2;
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
