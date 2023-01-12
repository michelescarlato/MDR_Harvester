using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Who;

public class WHOProcessor : IStudyProcessor
{
    IMonitorDataLayer _mon_repo;
    LoggingHelper _logger;

    public WHOProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger)
    {
        _mon_repo = mon_repo;
        _logger = logger;
    }

    public Study? ProcessData(string json_string, DateTime? download_datetime)
    {
        // set up json reader and deserialise file to a BioLiNCC object.

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        WHORecord? b = JsonSerializer.Deserialize<WHORecord?>(json_string, json_options);
        if (b is not null)
        {
            Study s = new Study();

            // get date retrieved in object fetch
            // transfer to study and data object records

            List<StudyIdentifier> study_identifiers = new List<StudyIdentifier>();
            List<StudyTitle> study_titles = new List<StudyTitle>();
            List<MDR_Harvester.StudyFeature> study_features = new List<MDR_Harvester.StudyFeature>();
            List<StudyTopic> study_topics = new List<StudyTopic>();
            List<StudyContributor> study_contributors = new List<StudyContributor>();
            List<StudyCountry> countries = new List<StudyCountry>();

            List<DataObject> data_objects = new List<DataObject>();
            List<ObjectTitle> data_object_titles = new List<ObjectTitle>();
            List<ObjectDate> data_object_dates = new List<ObjectDate>();
            List<ObjectInstance> data_object_instances = new List<ObjectInstance>();

            MD5Helpers hh = new MD5Helpers();
            IdentifierHelpers ih = new IdentifierHelpers();

            string sid = b.sd_sid!;  
            s.sd_sid = sid;
            s.datetime_of_data_fetch = download_datetime;

            int? source_id = b.source_id;
            string? source_name = ih.get_source_name(source_id);

            SplitDate? registration_date = null;
            string? date_registration = b.date_registration;  
            if (!string.IsNullOrEmpty(date_registration))
            {
                registration_date = date_registration.GetDatePartsFromISOString();
            }

            study_identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", source_id,
                                     source_name, registration_date?.date_string, null));

            // titles
            string? public_title = b.public_title;
            string? scientific_title = b.scientific_title;
            bool public_title_present = public_title?.AppearsGenuineTitle() is null ? false : (bool)public_title!.AppearsGenuineTitle();
            bool scientific_title_present = scientific_title?.AppearsGenuineTitle() is null ? false : (bool)scientific_title!.AppearsGenuineTitle();
            string source_string = "From the " + source_name;

            if (public_title_present)
            {
                public_title = public_title.ReplaceApos();
            }

            if (scientific_title_present)
            {
                scientific_title = scientific_title.ReplaceApos();
            }

            if (!public_title_present)
            {
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
                // public title available 

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

            s.title_lang_code = "en";  // as a default

            // need a mechanism, here to try and identify at least major language variations
            // e.g. Spanish, German, French - may be linkable to the source registry

            // brief description

            string? interventions = b.interventions;
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


            string? primary_outcome = b.primary_outcome;
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


            string? design_string = b.design_string;
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

            string? ipd_description = b.ipd_description;
            if (!string.IsNullOrEmpty(ipd_description)
                && ipd_description.Length > 10
                && ipd_description.ToLower() != "not available"
                && ipd_description.ToLower() != "not avavilable"
                && ipd_description.ToLower() != "not applicable"
                && !ipd_description.Contains("justification or reason for"))
            {
                s.data_sharing_statement = ipd_description.StringClean();
            }

            string? date_enrolment = b.date_enrolment;
            if (!string.IsNullOrEmpty(date_enrolment))
            {
                SplitDate? enrolment_date = date_enrolment.GetDatePartsFromISOString();
                if (enrolment_date is not null && enrolment_date?.year > 1960)
                {
                    s.study_start_year = enrolment_date.year;
                    s.study_start_month = enrolment_date.month;
                }
            }


            // study type and status 
            string? study_type = b.study_type;
            string? study_status = b.study_status;

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
                string? results_actual_enrollment = b.results_actual_enrollment;

                if (!string.IsNullOrEmpty(results_actual_enrollment)
                    && !results_actual_enrollment.Contains("9999")
                    && !Regex.Match(results_actual_enrollment, @"\d{4}-\d{2}-\d{2}").Success)
                {
                    enrolment = results_actual_enrollment;
                }
            }
            if (enrolment is null)
            {
                string? target_size = b.target_size;
                if (!string.IsNullOrEmpty(target_size)
                    && !target_size.Contains("9999"))
                {
                    enrolment = target_size;
                }
            }

            s.study_enrolment = enrolment;

            string? agemin = b.agemin;
            string? agemin_units = b.agemin_units;
            string? agemax = b.agemax;
            string? agemax_units = b.agemax_units;

            if (agemin is not null && Int32.TryParse(agemin, out int min))
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

            string? gender = b.gender;
            if (gender is not null && gender.Contains("?? Unable to classify"))
            {
                gender = "Not provided";
            }
            s.study_gender_elig = gender;
            s.study_gender_elig_id = gender.GetGenderEligId();

            // Add study attribute records.

            // study contributors - Sponsor N.B. default below
            string? sponsor_name = "No organisation name provided in source data";
            bool? sponsor_is_org = null;

            string? primary_sponsor = b.primary_sponsor;
            if (!string.IsNullOrEmpty(primary_sponsor))
            {
                sponsor_name = primary_sponsor.TidyOrgName(sid);
                string? sponsor = sponsor_name?.ToLower();
                if (sponsor is not null)
                {
                    if (sponsor.AppearsGenuineOrgName())
                    {
                        sponsor_is_org = true;
                        study_contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", null, sponsor_name));
                    }
                    else if (sponsor.StartsWith("dr ") || sponsor.StartsWith("prof ")
                            || sponsor.StartsWith("professor "))
                    {
                        string person_name = sponsor_name;
                        sponsor_is_org = false;
                        study_contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", person_name, null, null));
                    }
                }
            }


            string? funders = b.source_support;
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


            // Study lead
            string? study_lead = "";
            string? s_givenname = b.scientific_contact_givenname;
            string? s_familyname = b.scientific_contact_familyname;
            string? s_affiliation = b.scientific_contact_affiliation;
            if (!string.IsNullOrEmpty(s_givenname) || !string.IsNullOrEmpty(s_familyname))
            {
                string? full_name = (s_givenname + " " + s_familyname).Trim();
                full_name = full_name?.ReplaceApos();
                study_lead = full_name;  // for later comparison

                if (full_name.CheckPersonName())
                {
                    full_name = full_name.TidyPersonName();
                    if (full_name != "")
                    {
                        s_affiliation = s_affiliation.TidyOrgName(sid);
                        string? affil_org = s_affiliation?.ExtractOrganisation(sid);
                        study_contributors.Add(new StudyContributor(sid, 51, "Study Lead", full_name, s_affiliation, affil_org));
                    }
                }
            }

            // public contact
            string? p_givenname = b.public_contact_givenname;
            string? p_familyname = b.public_contact_familyname;
            string? p_affiliation = b.public_contact_affiliation;
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

            // study features 
            var study_feats = b.study_features;
            if (study_feats?.Any() == true)
            {
                foreach(var f in study_feats)
                { 
                    int? ftype_id = f.ftype_id;
                    string? ftype = f.ftype;
                    int? fvalue_id = f.fvalue_id;
                    string? fvalue = f.fvalue;
                    study_features.Add(new MDR_Harvester.StudyFeature(sid, ftype_id, ftype, fvalue_id, fvalue));
                }
            }


            // ************************************************************************
            // Most of this code to be transferred to WHO helper in Download system
            // ****
            // ************************************************************************
            // Most of this code to be transferred to WHO helper in Download system
            // ****

            //study identifiers
            var sids = b.secondary_ids;
            if (sids?.Any() == true)
            {
                foreach (var id in sids)
                {
                    int? sec_id_source = id.sec_id_source;
                    string? processed_id = id.processed_id;

                    // ************************************************************************
                    // Most of this code to be transferred to WHO helper in Download system
                    // ************************************************************************

                    if (sec_id_source == null && sponsor_name is not null)
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
                            // australian nz identifiers
                            if (processed_id.StartsWith("ADHB"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104531, "Aukland District Health Board"));
                            }

                            else if (processed_id.StartsWith("Auckland District Health Board"))
                            {
                                processed_id = processed_id.Replace("Auckland District Health Board", "");
                                processed_id = processed_id.Replace("registration", "").Replace("research", "");
                                processed_id = processed_id.Replace("number", "").Replace(":", "").Trim();
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104531, "Aukland District Health Board"));
                            }

                            else if (processed_id.StartsWith("AG01") || processed_id.StartsWith("AG02") || processed_id.StartsWith("AG03")
                                || processed_id.StartsWith("AG101") || processed_id.StartsWith("AGITG"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104532, "Australian Gastrointestinal Trials Group"));
                            }

                            else if (processed_id.Contains("Australasian Gastro-Intestinal Trials Group: "))
                            {
                                processed_id = processed_id.Replace("Australasian Gastro-Intestinal Trials Group:", "");
                                processed_id = processed_id.Replace("The", "").Replace("(AGITG)", "");
                                processed_id = processed_id.Replace("Protocol No:", "").Trim();
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104532, "Australian Gastrointestinal Trials Group"));
                            }

                            else if (processed_id.StartsWith("ALLG") || processed_id.StartsWith("AMLM"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103289, "Australasian Leukaemia and Lymphoma Group"));
                            }

                            else if (processed_id.Contains("Australasian Leukaemia and Lymphoma Group"))
                            {
                                processed_id = processed_id.Replace("Australasian Leukaemia and Lymphoma Group", "");
                                processed_id = processed_id.Replace("The", "").Replace("(ALLG):", "");
                                processed_id = processed_id.Replace(":", "").Replace("-", "").Trim();
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103289, "Australasian Leukaemia and Lymphoma Group"));
                            }

                            else if (processed_id.StartsWith("ANZGOG"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104533, "Australia New Zealand Gynaecological Oncology Group"));
                            }

                            else if (processed_id.StartsWith("Australia and New Zealand Gynecological Oncology Group: "))
                            {
                                processed_id = processed_id.Replace("Australia and New Zealand Gynecological Oncology Group:", "").Trim();
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104533, "Australia New Zealand Gynaecological Oncology Group"));
                            }

                            else if (processed_id.StartsWith("Australasian Sarcoma Study Group Number"))
                            {
                                processed_id = processed_id.Replace("Australasian Sarcoma Study Group Number", "").Trim();
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104534, "Australasian Sarcoma Study Group"));
                            }

                            else if (processed_id.StartsWith("ANZMTG"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104535, "Australia and New Zealand Melanoma Trials Group"));
                            }

                            else if (processed_id.StartsWith("ANZUP"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104536, "Australian and New Zealand Urogenital and Prostate Cancer Trials Group"));
                            }

                            else if (processed_id.StartsWith("APP") || processed_id.StartsWith("GNT"))
                            {
                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia"));
                            }

                            else if (processed_id.StartsWith("Australian NH&MRC"))
                            {
                                processed_id = processed_id.Replace("Australian NH&MRC", "");
                                processed_id = processed_id.Replace("Project", "").Replace("Grant", "");
                                processed_id = processed_id.Replace("Targeted Call for Research", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia"));
                            }

                            else if (processed_id.StartsWith("National Health and Medical Research Council") ||
                                        processed_id.StartsWith("National Health & Medical Research Council"))
                            {
                                processed_id = processed_id.Replace("National Health", "").Replace("Medical Research Council", "");
                                processed_id = processed_id.Replace("and", "").Replace("&", "").Replace("NHMRC", "");
                                processed_id = processed_id.Replace("application", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("grant", "", StringComparison.CurrentCultureIgnoreCase).Replace("ID", "");
                                processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase).Replace("No:", "");
                                processed_id = processed_id.Replace("project", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("(", "").Replace(")", "").Replace("funding", "").Replace("body", "");
                                processed_id = processed_id.Replace("of Australia", "").Replace("Postgraduate Scholarship", "");
                                processed_id = processed_id.Replace("protocol", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("Global Alliance for Chronic Diseases (GACD) initiative", "");
                                processed_id = processed_id.Replace(":", "").Replace(",", "").Replace("#", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia"));
                            }

                            else if (processed_id.StartsWith("NHMRCC"))
                            {
                                processed_id = processed_id.Replace("NHMRC", "");
                                processed_id = processed_id.Replace("grant", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("project", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("No:", "", StringComparison.CurrentCultureIgnoreCase).Replace("ID", "");
                                processed_id = processed_id.Replace("application", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("and FHF Centre for Research Excellence (CRE) in Diabetic Retinopathy App", "");
                                processed_id = processed_id.Replace("CIA_Campbell.", "");
                                processed_id = processed_id.Replace("partnership", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace(":", "").Replace(",", "").Replace("#", "").Replace("_", "").Replace(".", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 100690, "National Health and Medical Research Council, Australia"));
                            }

                            else if (processed_id.StartsWith("Australian Research Council"))
                            {
                                processed_id = processed_id.Replace("Australian Research Council", "");
                                processed_id = processed_id.Replace("(", "").Replace(")", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104537, "Australian Research Council"));
                            }

                            else if (processed_id.StartsWith("Australian Therapeutic Goods Administration"))
                            {
                                processed_id = processed_id.Replace("Australian Therapeutic Goods Administration", "");
                                processed_id = processed_id.Replace("Clinical Trial", "").Replace("TGA", "");
                                processed_id = processed_id.Replace("CTN", "").Replace("(", "").Replace(")", "");
                                processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("-", "").Replace(".", "").Replace("Notification", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN"));
                            }

                            else if (processed_id.Contains("Clinical Trial") && processed_id.Contains("CTN"))
                            {
                                processed_id = processed_id.Replace("Clinical Trial", "").Replace("TGA", "");
                                processed_id = processed_id.Replace("CTN", "").Replace("(", "").Replace(")", "").Replace(":", "");
                                processed_id = processed_id.Replace("Network", "").Replace("Notification", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN"));
                            }

                            else if (processed_id.StartsWith("TGA"))
                            {
                                processed_id = processed_id.Replace("Clinical", "").Replace("TGA", "").Replace("CTN", "");
                                processed_id = processed_id.Replace("trial", "", StringComparison.CurrentCultureIgnoreCase)
                                                            .Replace("trials", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace(":", "").Replace("Notification", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN"));
                            }

                            else if (processed_id.StartsWith("Therapeutic good administration") ||
                                        processed_id.StartsWith("Therapeutic Goods Administration") ||
                                        processed_id.StartsWith("Therapeutic Goods Association"))
                            {
                                processed_id = processed_id.Replace("Therapeutic", "").Replace("goods", "").Replace("Goods", "");
                                processed_id = processed_id.Replace("TGA", "").Replace("CTN", "");
                                processed_id = processed_id.Replace("administration", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("association", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("clinical", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("trial", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("notification", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("protocol", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("reference", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("no.", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace("Australian Govt, Dept of Health -", "");
                                processed_id = processed_id.Replace("scheme", "", StringComparison.CurrentCultureIgnoreCase);
                                processed_id = processed_id.Replace(":", "").Replace("(", "").Replace(")", "").Replace(",", "").Trim();
                                if (processed_id.StartsWith("-")) processed_id = processed_id.Substring(1).Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 41, "Regulatory body ID", 104538, "Australian Therapeutic Goods Administration CTN"));
                            }

                            else if (processed_id.StartsWith("CRG") || processed_id.StartsWith("Cochrane Renal Group"))
                            {
                                processed_id = processed_id.Replace("Cochrane Renal Group", "").Replace("CRG", "");
                                processed_id = processed_id.Replace("(", "").Replace(")", "").Replace("-", "").Replace(".", "").Replace(":", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, "CRG" + processed_id, 43, "Collaborative Group ID", 104539, "Cochrane Renal Group"));
                            }

                            else if (processed_id.StartsWith("Commonwealth Scientific and Industrial Research Organisation"))
                            {
                                processed_id = processed_id.Replace("Commonwealth Scientific and Industrial Research Organisation", "");
                                processed_id = processed_id.Replace("(CSIRO)", "").Replace(":", "").Replace("and", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104540, "Commonwealth Scientific and Industrial Research Organisation"));
                            }

                            else if (processed_id.StartsWith("Health Research Council") && !processed_id.Contains("Funding"))
                            {
                                processed_id = processed_id.Replace("Health Research Council", "");
                                processed_id = processed_id.Replace("of New Zealand", "");
                                processed_id = processed_id.Replace("NZ", "").Replace("HRC", "");
                                processed_id = processed_id.Replace("reference", "", StringComparison.CurrentCultureIgnoreCase).Replace("Ref.", "");
                                processed_id = processed_id.Replace("number", "", StringComparison.CurrentCultureIgnoreCase).Replace("programme", "");
                                processed_id = processed_id.Replace("grant", "").Trim();

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104541, "Health Research Council of New Zealand"));
                            }

                            else if (processed_id.StartsWith("HRC"))
                            {
                                processed_id = processed_id.Replace("HRC", "");
                                processed_id = processed_id.Replace("Emerging Research First Grant- ", "");
                                processed_id = processed_id.Replace("Project Grant Number #", "");
                                processed_id = processed_id.Replace("Ref:", "").Trim(); ;

                                study_identifiers.Add(new StudyIdentifier(sid, processed_id, 13, "Funder’s ID", 104541, "Health Research Council of New Zealand"));
                            }

                                else if (processed_id.StartsWith("HREC"))
                                {
                                    if (sponsor_is_org == true)
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name));
                                    }
                                    else
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 12, "No organisation name provided in source data"));
                                    }

                                }

                                else if (processed_id.StartsWith("Human Research Ethics Committee (HREC):"))
                                {
                                    // ??? change type and keep sponsor
                                    processed_id = processed_id.Replace("Human Research Ethics Committee (HREC):", "");

                                    if (sponsor_is_org == true)
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", null, sponsor_name));
                                    }
                                    else
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 12, "Ethics review ID", 12, "No organisation name provided in source data"));
                                    }
                                }

                                else if (processed_id.StartsWith("MRINZ"))
                                {
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 103010, "Medical Research Institute of New Zealand"));
                                }

                                else if (processed_id.StartsWith("National Clinical Trials Registry"))
                                {
                                    processed_id = processed_id.Replace("National Clinical Trials Registry:", "").Trim();
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial registry ID", 104548, "National Clinical Trials Registry (Australia)"));
                                }
                                else if (processed_id.StartsWith("Perinatal Trials Registry:"))
                                {
                                    processed_id = processed_id.Replace("Perinatal Trials Registry:", "").Trim();
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial registry ID", 104542, "Perinatal Trials Registry (Australia)"));
                                }

                                else if (processed_id.StartsWith("TROG"))
                                {
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 43, "Collaborative Group ID", 104543, "Trans Tasman Radiation Oncology Group"));
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
                            else if (source_id == 100118)
                            {
                                if (!processed_id.EndsWith("#32"))    // first ignore these (small sub group)
                                {
                                    if (processed_id.StartsWith("AMCTR"))
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104544, "Acupuncture-Moxibustion Clinical Trial Registry"));
                                    }
                                    else if (processed_id.StartsWith("ChiMCTR"))
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104545, "Chinese Medicine Clinical Trials Registry"));
                                    }
                                    else if (processed_id.StartsWith("CUHK"))
                                    {
                                        study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104546, "CUHK Clinical Research and Biostatistics Clinical Trials Registry"));
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

                            else if (source_id == 100127)
                            {
                                if (processed_id.StartsWith("JapicCTI"))
                                {
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100157, "Japan Pharmaceutical Information Center"));
                                }
                                else if (processed_id.StartsWith("JMA"))
                                {
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100158, "Japan Medical Association – Center for Clinical Trials"));
                                }
                                else if (processed_id.StartsWith("jCRT") || processed_id.StartsWith("JCRT"))
                                {
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 104547, "Japan Registry of Clinical Trials"));
                                }
                                else if (processed_id.StartsWith("UMIN"))
                                {
                                    processed_id = processed_id.Replace("ID", "").Replace("No", "").Replace(":", "").Replace(".  ", "").Trim();
                                    study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", 100156, "University Hospital Medical Information Network CTR"));
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
                    else
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
                            study_identifiers.Add(new StudyIdentifier(sid, processed_id, 11, "Trial Registry ID", sec_id_source, source_name));
                        }
                    }
                }
            }

            // study conditions
            var condList = b.condition_list;
            if (condList?.Any() == true)
            {
                foreach (var cn in condList)
                {
                    string? condition = cn.condition;
                    if (!string.IsNullOrEmpty(condition))
                    {
                        char[] chars_to_trim = { ' ', '?', ':', '*', '/', '-', '_', '+', '=', '>', '<', '&' };
                        string cond = condition.Trim(chars_to_trim);
                        if (!string.IsNullOrEmpty(cond) && cond.ToLower() != "not applicable" && cond.ToLower() != "&quot")
                        {
                            if (topic_is_new(cond))
                            {
                                string? code = cn.code;
                                string? code_system = cn.code_system;

                                if (code == null)
                                {
                                    study_topics.Add(new StudyTopic(sid, 13, "Condition", cond));
                                }
                                else
                                {
                                    if (code_system == "ICD 10")
                                    {
                                        study_topics.Add(new StudyTopic(sid, 13, "Condition", cond, 12, code));
                                    }
                                }
                            }
                        }
                    }
                }
            }


            bool topic_is_new(string candidate_topic)
            {
                foreach (StudyTopic k in study_topics)
                {
                    if (k is not null && k.original_value.ToLower() == candidate_topic.ToLower())
                    {
                        return false;
                    }
                }
                return true;
            }


            // Create data object records.
            // registry entry

            string name_base = s.display_title;
            string reg_prefix = ih.get_registry_prefix(source_id);
            string object_title = reg_prefix + "registry web page";
            string object_display_title = name_base + " :: " + reg_prefix + "registry web page";
            string sd_oid = sid + " :: 13 :: " + object_title;

            int? pub_year = registration_date?.year;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, pub_year, 23, "Text", 13, "Trial Registry entry",
                source_id, source_name, 12, download_datetime));

            data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                "Study short name :: object type", true));

            string? remote_url = b.remote_url;
            data_object_instances.Add(new ObjectInstance(sd_oid, source_id, source_name,
                                remote_url, true, 35, "Web text"));

            if (registration_date is not null)
            {
                data_object_dates.Add(new ObjectDate(sd_oid, 15, "Created", registration_date.year,
                          registration_date.month, registration_date.day, registration_date.date_string));
            }

            string? rec_date = b.record_date;
            if (!string.IsNullOrEmpty(rec_date))
            {
                SplitDate? record_date = rec_date.GetDatePartsFromISOString();
                data_object_dates.Add(new ObjectDate(sd_oid, 18, "Updated", record_date?.year,
                          record_date?.month, record_date?.day, record_date?.date_string));

            }

            // there may be (rarely) a results link...usually but not always back tothe source registry 
            // also rarely a results_url_protocol - meaning unclear

            string? results_url_link = b.results_url_link;
            string? results_url_protocol = b.results_url_protocol;
            string? results_date_posted = b.results_date_posted;
            string? results_date_completed = b.results_date_completed;

            SplitDate? results_posted_date = results_date_posted?.GetDatePartsFromISOString();
            SplitDate? results_completed_date = results_date_completed?.GetDatePartsFromISOString();

            if (!string.IsNullOrEmpty(results_url_link))
            {
                // (exclude those on CTG - should be picked up there)

                string results_link = results_url_link.ToLower();
                if (results_link.Contains("http") && !results_link.Contains("clinicaltrials.gov"))
                {
                    object_title = "Results summary";
                    object_display_title = name_base + " :: " + "Results summary";
                    sd_oid = sid + " :: 28 :: " + object_title;

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
                        int pdf_end = prot_url.IndexOf(".pdf");
                        url_link = results_url_protocol.Substring(url_start, pdf_end - url_start + 4);

                    }
                    else if (prot_url.Contains(".doc"))
                    {
                        resource_type = "Word doc";
                        resource_type_id = 16;
                        if (prot_url.Contains(".docx"))
                        {
                            int docx_end = prot_url.IndexOf(".docx");
                            url_link = results_url_protocol.Substring(url_start, docx_end - url_start + 5);
                        }
                        else
                        {
                            int doc_end = prot_url.IndexOf(".doc");
                            url_link = results_url_protocol.Substring(url_start, doc_end - url_start + 4);
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
                        // most likely... but difficult to tell
                        object_type_id = 79;
                        object_type = "CSR summary";
                    }

                    object_display_title = name_base + " :: " + object_type;
                    object_title = object_type;
                    sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                    // almost certainly not in or managed by the registry

                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, pub_year, 23, "Text", object_type_id, object_type,
                    null, null, 11, download_datetime));

                    data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                        "Study short name :: object type", true));

                    data_object_instances.Add(new ObjectInstance(sd_oid, null, null,
                                        url_link, true, resource_type_id, resource_type));
                }
            }


            var countryList = b.country_list;
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
                                if (ih.CheckIfOrganisation(fullname))
                                {
                                    sc.organisation_name = sh.TidyOrgName(sid, sc.person_full_name);
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
                                if (ih.CheckIfIndividual(orgname))
                                {
                                    sc.person_full_name = sh.TidyPersonName(sc.organisation_name);
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
            s.topics = study_topics;
            s.contributors = study_contributors;
            s.countries = countries;

            s.data_objects = data_objects;
            s.object_titles = data_object_titles;
            s.object_dates = data_object_dates;
            s.object_instances = data_object_instances;

            return s;
        }
        else
        {
            return null;
        }
    }
}
