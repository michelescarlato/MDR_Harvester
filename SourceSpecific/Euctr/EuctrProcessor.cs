using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace MDR_Harvester.Euctr;

public class EUCTRProcessor : IStudyProcessor
{
    IMonitorDataLayer _mon_repo;
    LoggingHelper _logger;

    public EUCTRProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger)
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

        EUCTR_Record? b = JsonSerializer.Deserialize<EUCTR_Record?>(json_string, json_options);
        if (b is not null)
        {
            Study s = new Study();
            List<StudyIdentifier> identifiers = new();
            List<StudyTitle> titles = new();
            List<StudyContributor> contributors = new();
            List<StudyTopic> topics = new();
            List<StudyFeature> features = new();
            List<StudyLocation> sites = new();
            List<StudyCountry> countries = new();

            List<DataObject> data_objects = new();
            List<ObjectTitle> object_titles = new();
            List<ObjectInstance> object_instances = new();
            List<ObjectDate> object_dates = new();

            List<IMP> imp_list = new();

            MD5Helpers hh = new();
            DateHelpers dh = new();
            IdentifierHelpers ih = new();
            TypeHelpers th = new();

            string study_description = null;

            // First convert the XML document to a Linq XML Document.

            XDocument xDoc = XDocument.Load(new XmlNodeReader(d));

            // Obtain the main top level elements of the registry entry.

            XElement r = xDoc.Root;

            string sid = GetElementAsString(r.Element("eudract_id"));
            s.sd_sid = sid;
            s.datetime_of_data_fetch = download_datetime;


            // By defintion with the EU CTR
            s.study_type = "Interventional";
            s.study_type_id = 11;

            s.study_status = GetElementAsString(r.Element("trial_status"));
            switch (s.study_status)
            {
                case "Ongoing":
                    {
                        s.study_status = "Ongoing";
                        s.study_status_id = 25;
                        break;
                    }
                case "Completed":
                    {
                        s.study_status = "Completed";
                        s.study_status_id = 21;
                        break;
                    }
                case "Prematurely Ended":
                    {
                        s.study_status = "Terminated";
                        s.study_status_id = 22;
                        break;
                    }
                case "Temporarily Halted":
                    {
                        s.study_status = "Suspended";
                        s.study_status_id = 18;
                        break;
                    }
                case "Not Authorised":
                    {
                        s.study_status = "Withdrawn";
                        s.study_status_id = 11;
                        break;
                    }
                default:
                    {
                        s.study_status_id = 0;
                        break;
                    }
            }


            // study start year and month
            // public string start_date { get; set; }  in yyyy-MM-dddd format
            string start_date = GetElementAsString(r.Element("start_date"));
            if (DateTime.TryParseExact(start_date, "yyyy-MM-dd", new CultureInfo("en-UK"), DateTimeStyles.AssumeLocal, out DateTime start))
            {
                s.study_start_year = start.Year;
                s.study_start_month = start.Month;
            }

            // contributor - sponsor
            string sponsor_name = "No organisation name provided in source data";
            string sponsor = GetElementAsString(r.Element("sponsor_name"));
            if (sh.AppearsGenuineOrgName(sponsor))
            {
                sponsor_name = sh.TidyOrgName(sponsor, sid);
                string lower_sponsor = sponsor_name.ToLower();
                if (!string.IsNullOrEmpty(lower_sponsor) && lower_sponsor.Length > 1
                    && lower_sponsor != "dr" && lower_sponsor != "no profit")
                {
                    contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", null, sponsor_name));
                }
            }

            // may get funders or other supporting orgs
            var sponsors = r.Element("sponsors");
            if (sponsors != null)
            {
                var detail_lines = sponsors.Elements("DetailLine");
                if (detail_lines != null && detail_lines.Count() > 0)
                {
                    foreach (XElement dline in detail_lines)
                    {
                        string item_name = GetElementAsString(dline.Element("item_name"));
                        if (item_name == "Name of organisation providing support")
                        {
                            var values = dline.Elements("values");
                            if (values != null && values.Count() > 0)
                            {
                                string org_value = GetElementAsString(values.First());
                                // check a funder is not simply the sponsor...
                                if (sh.AppearsGenuineOrgName(org_value))
                                {
                                    string funder = sh.TidyOrgName(org_value, sid);
                                    if (funder != sponsor_name)
                                    {
                                        string fund = funder.ToLower();
                                        if (!string.IsNullOrEmpty(fund) && fund.Length > 1
                                        && fund != "dr" && fund != "no profit")
                                        {
                                            contributors.Add(new StudyContributor(sid, 58, "Study Funder", null, funder));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            // study identifiers
            // do the eu ctr id first...
            identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100123, "EU Clinical Trials Register", null, null));

            // do the sponsor's id
            string sponsor_id = GetElementAsString(r.Element("sponsor_id"));

            if (!string.IsNullOrEmpty(sponsor_id))
            {
                if (!string.IsNullOrEmpty(sponsor_name))
                {
                    identifiers.Add(new StudyIdentifier(sid, sponsor_id, 14, "Sponsor ID", null, sponsor_name, null, null));
                }
                else
                {
                    identifiers.Add(new StudyIdentifier(sid, sponsor_id, 14, "Sponsor ID", 12, "No organisation name provided in source data", null, null));
                }
            }


            // identifier section actually seems to have titles
            var idents = r.Element("identifiers");
            if (idents != null)
            {
                string second_language = "";
                var detail_lines = idents.Elements("DetailLine");
                if (detail_lines != null && detail_lines.Count() > 0)
                {
                    foreach (XElement dline in detail_lines)
                    {
                        string item_code = GetElementAsString(dline.Element("item_code"));
                        switch (item_code)
                        {
                            case "A.1":
                                {
                                    // 'member state concerned'
                                    // used here to estimate any non Englilsh title text listed
                                    var values = dline.Elements("values");
                                    if (values != null)
                                    {
                                        string member_state = GetElementAsString(values.First());
                                        if (member_state.ToLower().Contains("spain")) second_language = "es";
                                        else if (member_state.ToLower().Contains("portug")) second_language = "pt";
                                        else if (member_state.ToLower().Contains("france") || member_state.ToLower().Contains("french")) second_language = "fr";
                                        else if (member_state.ToLower().Contains("german")) second_language = "de";
                                        else if (member_state.ToLower().Contains("ital")) second_language = "it";
                                        else if (member_state.ToLower().Contains("dutch") || member_state.ToLower().Contains("neder") || member_state.ToLower().Contains("nether")) second_language = "nl";
                                        else if (member_state.ToLower().Contains("danish") || member_state.ToLower().Contains("denm")) second_language = "da";
                                        else if (member_state.ToLower().Contains("swed")) second_language = "sv";
                                        else if (member_state.ToLower().Contains("norw")) second_language = "no";
                                        else if (member_state.ToLower().Contains("fin")) second_language = "fi";
                                        else if (member_state.ToLower().Contains("polish")) second_language = "pl";
                                        else if (member_state.ToLower().Contains("hung")) second_language = "hu";
                                        else if (member_state.ToLower().Contains("czech")) second_language = "cs";
                                        else if (member_state.ToLower().Contains("slovak")) second_language = "sk";
                                        else if (member_state.ToLower().Contains("sloven")) second_language = "sl";
                                        else if (member_state.ToLower().Contains("greece") || member_state.ToLower().Contains("greek")) second_language = "el";
                                        else if (member_state.ToLower().Contains("cypr")) second_language = "el";
                                    }
                                    break;
                                }

                            case "A.3":
                                {
                                    // may be multiple (but may just repeat)
                                    var values = dline.Elements("values");
                                    if (values != null)
                                    {
                                        var indiv_values = values.Elements("value");
                                        if (indiv_values != null && indiv_values.Count() > 0)
                                        {
                                            int indiv_value_num = 0;
                                            foreach (XElement v in indiv_values)
                                            {
                                                string name = GetElementAsString(v);
                                                indiv_value_num++;
                                                if (name != null && name.Length >= 4)
                                                {
                                                    string st_name = name.Trim().ToLower();
                                                    if (sh.AppearsGenuineTitle(name))
                                                    {
                                                        name = name.ReplaceApos();
                                                        if (!NameAlreadyPresent(name))
                                                        {
                                                            if (indiv_value_num == 1)
                                                            {
                                                                titles.Add(new StudyTitle(sid, name, 16, "Registry scientific title", "en", 11, false, "From the EU CTR"));

                                                            }
                                                            else
                                                            {
                                                                titles.Add(new StudyTitle(sid, name, 16, "Registry scientific title", second_language, 22, false, "From the EU CTR"));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "A.3.1":
                                {
                                    // may be multiple (but may just repeat)
                                    var values = dline.Elements("values");
                                    if (values != null)
                                    {
                                        var indiv_values = values.Elements("value");
                                        if (indiv_values != null && indiv_values.Count() > 0)
                                        {
                                            int indiv_value_num = 0;
                                            foreach (XElement v in indiv_values)
                                            {
                                                string name = GetElementAsString(v);
                                                if (name != null && name.Length >= 4)
                                                {
                                                    if (sh.AppearsGenuineTitle(name))
                                                    {
                                                        indiv_value_num++;
                                                        name = name.ReplaceApos();
                                                        if (!NameAlreadyPresent(name))
                                                        {
                                                            if (indiv_value_num == 1)
                                                            {
                                                                titles.Add(new StudyTitle(sid, name, 15, "Registry public title", "en", 11, true, "From the EU CTR"));
                                                            }
                                                            else
                                                            {
                                                                titles.Add(new StudyTitle(sid, name, 15, "Registry public title", second_language, 22, false, "From the EU CTR"));
                                                            }
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
                                    var values = dline.Elements("values");

                                    // may be multiple 
                                    if (values != null)
                                    {
                                        var indiv_values = values.Elements("value");
                                        if (indiv_values != null && indiv_values.Count() > 0)
                                        {
                                            foreach (XElement v in indiv_values)
                                            {
                                                string acronym = GetElementAsString(v);
                                                string name = acronym.Trim().ToLower();
                                                if (!name.StartsWith("not ") && !name.StartsWith("non ") && name.Length > 2 &&
                                                    name != "n/a" && name != "n.a." && name != "none" && !name.StartsWith("no ap")
                                                    && !name.StartsWith("no av"))
                                                {
                                                    name = sh.ReplaceApos(name);
                                                    if (!NameAlreadyPresent(name) && name.Length < 20)
                                                    {
                                                        titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From the EU CTR"));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "A.2":
                                {
                                    // do nothing
                                    break;
                                }
                            case "A.4.1":
                                {
                                    // do nothing - already have sponsor id
                                    break;
                                }
                            case "A.5.1":
                                {
                                    // identifier: ISRCTN (International Standard Randomised Controlled Trial) Number
                                    var values = dline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        string isrctn_id = GetElementAsString(values.First());
                                        if (isrctn_id.ToLower().StartsWith("isrctn"))
                                        {
                                            identifiers.Add(new StudyIdentifier(sid, isrctn_id, 11, "Trial Registry ID",
                                                100126, "ISRCTN", null, null));
                                        }
                                    }
                                    break;
                                }
                            case "A.5.2":
                                {
                                    // identifier: NCT Number
                                    var values = dline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        string nct_id = GetElementAsString(values.First());
                                        if (nct_id.ToLower().StartsWith("nct"))
                                        {
                                            identifiers.Add(new StudyIdentifier(sid, nct_id, 11, "Trial Registry ID",
                                                100120, "ClinicalTrials.gov", null, null));
                                        }
                                    }
                                    break;
                                }
                            case "A.5.3":
                                {
                                    // identifier: WHO UTN Number
                                    var values = dline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        string who_id = GetElementAsString(values.First());
                                        if (who_id.ToLower().StartsWith("u1111"))
                                        {
                                            identifiers.Add(new StudyIdentifier(sid, who_id, 11, "Trial Registry ID",
                                                100115, "International Clinical Trials Registry Platform", null, null));
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    break;    // nothing left of any significance - do nothing
                                }
                        }
                    }
                }
            }


            bool NameAlreadyPresent(string candidate_name)
            {
                bool res = false;
                foreach (StudyTitle t in titles)
                {
                    if (t.title_text.ToLower() == candidate_name.ToLower())
                    {
                        res = true;
                        break;
                    }
                }
                return res;
            }

            // ensure a default and display title
            bool display_title_exists = false;
            for (int k = 0; k < titles.Count; k++)
            {
                if (titles[k] is not null)
                {
                    if ((bool)titles[k].is_default)
                    {
                        s.display_title = titles[k].title_text;
                        display_title_exists = true;
                        break;
                    }
                }
            }


            if (!display_title_exists)
            {
                // use the registry title - should always be one
                for (int k = 0; k < titles.Count; k++)
                {
                    if (titles[k].title_type_id == 16)
                    {
                        titles[k].is_default = true;
                        s.display_title = titles[k].title_text;
                        display_title_exists = true;
                        break;
                    }
                }
            }

            if (!display_title_exists)
            {
                // use an acronym
                for (int k = 0; k < titles.Count; k++)
                {
                    if (titles[k].title_type_id == 14)
                    {
                        titles[k].is_default = true;
                        s.display_title = titles[k].title_text;
                        display_title_exists = true;
                        break;
                    }
                }
            }


            // add in an explanatory message... if no title
            if (!display_title_exists)
            {
                s.display_title = sid + " (No meaningful title provided)";
            }

            // truncate title if too long
            if (s.display_title.Length > 400)
            {
                s.display_title = s.display_title.Substring(0, 400) + "...";
            }

            // study design info

            var feats = r.Element("features");
            if (feats != null)
            {
                var detail_lines = feats.Elements("DetailLine");
                if (detail_lines?.Any() == true)
                {
                    foreach (XElement dline in detail_lines)
                    {
                        string item_code = GetElementAsString(dline.Element("item_code"));
                        switch (item_code)
                        {
                            case "E.1.1":
                                {
                                    // conditions under study
                                    var values = dline.Elements("values");
                                    if (values != null)
                                    {
                                        var indiv_values = values.Elements("value");
                                        if (indiv_values != null && indiv_values.Count() > 0)
                                        {
                                            foreach (XElement value in indiv_values)
                                            {
                                                string name = GetElementAsString(value);
                                                if (!name.Contains("\r") && !name.Contains("\n") && name.Length < 100)
                                                {
                                                    topics.Add(new StudyTopic(sid, 13, "condition", name));
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "E.2.1":
                                {
                                    // primary objectives -  may be multiple (i.e. in 2 languages, but may just repeat)
                                    var values = dline.Elements("values");
                                    if (values != null)
                                    {
                                        var indiv_values = values.Elements("value");
                                        if (indiv_values != null && indiv_values.Count() > 0)
                                        {
                                            int indiv_value_num = 0;
                                            string study_objectives = null;

                                            foreach (XElement v in indiv_values)
                                            {
                                                string? primary_obs = GetElementAsString(v);
                                                primary_obs = primary_obs?.StringClean();
                                                indiv_value_num++;

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
                                                        study_objectives += "\n(" + primary_obs + ")";
                                                    }
                                                }
                                            }

                                            if (study_objectives != null)
                                            {
                                                study_description = study_objectives;
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "E.5.1":
                                {
                                    // primary end points
                                    var values = dline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        int indiv_value_num = 0;
                                        string study_endpoints = null;

                                        foreach (XElement v in values)
                                        {
                                            string? end_points = GetElementAsString(v);
                                            end_points = end_points?.StringClean();
                                            indiv_value_num++;

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

                                        if (study_endpoints != null)
                                        {
                                            study_description += string.IsNullOrEmpty(study_description) ? study_endpoints : "\n" + study_endpoints;
                                        }
                                    }
                                    break;

                                }
                            case "E.7.1":
                                {
                                    // Phase 1
                                    features.Add(new StudyFeature(sid, 20, "phase", 110, "Phase 1"));
                                    break;
                                }
                            case "E.7.2":
                                {
                                    // Phase 2
                                    features.Add(new StudyFeature(sid, 20, "phase", 120, "Phase 2"));
                                    break;
                                }
                            case "E.7.3":
                                {
                                    // Phase 3
                                    features.Add(new StudyFeature(sid, 20, "phase", 130, "Phase 3"));
                                    break;
                                }
                            case "E.7.4":
                                {
                                    // Phase 4
                                    features.Add(new StudyFeature(sid, 20, "phase", 135, "Phase 4"));
                                    break;
                                }
                            case "E.8.1":
                                {
                                    // Controlled - do nothing
                                    break;
                                }
                            case "E.8.1.1":
                                {
                                    // Randomised
                                    features.Add(new StudyFeature(sid, 22, "allocation type", 205, "Randomised"));
                                    break;
                                }
                            case "E.8.1.2":
                                {
                                    // open
                                    features.Add(new StudyFeature(sid, 24, "masking", 500, "None (Open Label)"));
                                    break;
                                }
                            case "E.8.1.3":
                                {
                                    // Single blindd
                                    features.Add(new StudyFeature(sid, 24, "masking", 505, "Single"));
                                    break;
                                }
                            case "E.8.1.4":
                                {
                                    // Double blind
                                    features.Add(new StudyFeature(sid, 24, "masking", 510, "Double"));
                                    break;
                                }
                            case "E.8.1.5":
                                {
                                    // Parallel group
                                    features.Add(new StudyFeature(sid, 23, "intervention model", 305, "Parallel assignment"));
                                    break;
                                }
                            case "E.8.1.6":
                                {
                                    // Crossover
                                    features.Add(new StudyFeature(sid, 23, "intervention model", 310, "Crossover assignment"));
                                    break;
                                }
                            default:
                                {
                                    // do nothing
                                    break;
                                }
                        }
                    }
                }
            }

            // eligibility

            var population = r.Element("population");
            if (population != null)
            {
                var detail_lines = population.Elements("DetailLine");
                if (detail_lines?.Any() == true)
                {

                    bool includes_under18 = false;
                    bool includes_in_utero = false, includes_preterm = false;
                    bool includes_newborns = false, includes_infants = false;
                    bool includes_children = false, includes_ados = false;
                    bool includes_adults = false, includes_elderly = false;
                    bool includes_women = false, includes_men = false;

                    foreach (XElement dline in detail_lines)
                    {
                        string item_code = GetElementAsString(dline.Element("item_code"));
                        switch (item_code)
                        {
                            case "F.1.1":
                                {
                                    // under 18
                                    includes_under18 = true; break;
                                }
                            case "F.1.1.1":
                                {
                                    includes_in_utero = true; break;
                                }
                            case "F.1.1.2":
                                {
                                    includes_preterm = true; break;
                                }
                            case "F.1.1.3":
                                {
                                    includes_newborns = true; break;
                                }
                            case "F.1.1.4":
                                {
                                    includes_infants = true; break;
                                }
                            case "F.1.1.5":
                                {
                                    includes_children = true; break;
                                }
                            case "F.1.1.6":
                                {
                                    includes_ados = true; break;
                                }

                            case "F.1.2":
                                {
                                    // Adults 18 - 64
                                    includes_adults = true; break;
                                }
                            case "F.1.3":
                                {
                                    // Elderly, >65
                                    includes_elderly = true; break;
                                }
                            case "F.2.1":
                                {
                                    includes_women = true; break;
                                }
                            case "F.2.2":
                                {
                                    includes_men = true; break;
                                }
                            default:
                                {
                                    break;    // nothing left of any significance - do nothing
                                }
                        }
                    }

                    if (includes_men && includes_women)
                    {
                        s.study_gender_elig = "All"; s.study_gender_elig_id = 900;
                    }
                    else if (includes_women)
                    {
                        s.study_gender_elig = "Female"; s.study_gender_elig_id = 905;
                    }
                    else if (includes_men)
                    {
                        s.study_gender_elig = "Male"; s.study_gender_elig_id = 910;
                    }


                    if (!includes_under18)
                    {
                        if (includes_adults && includes_elderly)
                        {
                            s.min_age = 18; s.min_age_units = "Years"; s.min_age_units_id = 17;
                        }
                        else if (includes_adults)
                        {
                            s.min_age = 18; s.min_age_units = "Years"; s.min_age_units_id = 17;
                            s.max_age = 64; s.max_age_units = "Years"; s.max_age_units_id = 17;
                        }
                        else if (includes_elderly)
                        {
                            s.min_age = 65; s.min_age_units = "Years"; s.min_age_units_id = 17;
                        }
                    }
                    else
                    {
                        if (includes_in_utero || includes_preterm || includes_newborns)
                        {
                            s.min_age = 0; s.min_age_units = "Days"; s.min_age_units_id = 14;
                        }
                        else if (includes_infants)
                        {
                            s.min_age = 28; s.min_age_units = "Days"; s.min_age_units_id = 14;
                        }
                        else if (includes_children)
                        {
                            s.min_age = 2; s.min_age_units = "Years"; s.min_age_units_id = 17;
                        }
                        else if (includes_ados)
                        {
                            s.min_age = 12; s.min_age_units = "Years"; s.min_age_units_id = 17;
                        }


                        if (includes_adults)
                        {
                            s.max_age = 64; s.max_age_units = "Years"; s.max_age_units_id = 17;
                        }
                        else if (includes_ados)
                        {
                            s.max_age = 17; s.max_age_units = "Years"; s.max_age_units_id = 17;
                        }
                        else if (includes_children)
                        {
                            s.max_age = 11; s.max_age_units = "Years"; s.max_age_units_id = 17;
                        }
                        else if (includes_infants)
                        {
                            s.max_age = 23; s.max_age_units = "Months"; s.max_age_units_id = 16;
                        }
                        else if (includes_newborns)
                        {
                            s.max_age = 27; s.max_age_units = "Days"; s.max_age_units_id = 14;
                        }
                        else if (includes_in_utero || includes_preterm)
                        {
                            s.max_age = 0; s.max_age_units = "Days"; s.max_age_units_id = 14;
                        }
                    }
                }
            }


            // topics (mostly IMPs)

            var imps = r.Element("imps");
            if (imps != null)
            {
                var imp_lines = imps.Elements("ImpLine");
                if (imp_lines?.Any() == true)
                {
                    int current_num = 0;
                    IMP imp = null;
                    foreach (XElement iline in imp_lines)
                    {
                        int imp_num = (int)GetElementAsInt(iline.Element("imp_number"));

                        if (imp_num > current_num)
                        {
                            // new imp class required to hold the values found below
                            // store the old one first

                            if (current_num != 0) imp_list.Add(imp);
                            current_num = imp_num;
                            imp = new IMP(current_num);
                        }

                        string item_code = GetElementAsString(iline.Element("item_code"));

                        switch (item_code)
                        {
                            case "D.2.1.1.1":
                                {
                                    // Trade name
                                    var values = iline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        string topic_name = GetElementAsString(values.First());
                                        string name = topic_name.ToLower();
                                        if (name != "not available" && name != "n/a" && name != "na" && name != "not yet extablished")
                                        {
                                            imp.trade_name = topic_name.Replace(((char)174).ToString(), "");    // drop reg mark
                                        }
                                    }
                                    break;
                                }
                            case "D.3.1":
                                {
                                    // Product name
                                    var values = iline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        string topic_name = GetElementAsString(values.First());
                                        string name = topic_name.ToLower();
                                        if (name != "not available" && name != "n/a" && name != "na" && name != "not yet extablished")
                                        {
                                            imp.product_name = topic_name.Replace(((char)174).ToString(), "");    // drop reg mark
                                        }
                                    }
                                    break;
                                }
                            case "D.3.8":
                                {
                                    // INN
                                    var values = iline.Elements("values");
                                    if (values != null && values.Count() > 0)
                                    {
                                        string topic_name = GetElementAsString(values.First());
                                        string name = topic_name.ToLower();
                                        if (name != "not available" && name != "n/a" && name != "na" && name != "not yet extablished")
                                        {
                                            imp.inn = topic_name;
                                        }
                                    }
                                    break;
                                }
                            case "D.3.9.1":
                                {
                                    // CAS number, do nothing
                                    break;
                                }
                            case "D.3.9.3":
                                {
                                    // other descriptive name, do nothing
                                    break;
                                }
                            default:
                                {
                                    break;    // nothing left of any significance - do nothing
                                }
                        }
                    }

                    // add the last one 
                    if (current_num != 0) imp_list.Add(imp);

                    // process the imp list
                    if (imp_list.Count > 0)
                    {
                        // use the poroduct name, or the INN, or the trade name, in that order

                        foreach (IMP i in imp_list)
                        {
                            string imp_name = "";
                            if (i.product_name != null)
                            {
                                imp_name = i.product_name;
                            }
                            else if (i.inn != null)
                            {
                                imp_name = i.inn;
                            }
                            else if (i.trade_name != null)
                            {
                                imp_name = i.trade_name;
                            }

                            if (imp_name != "" && !IMPAlreadyThere(imp_name))
                            {
                                topics.Add(new StudyTopic(sid, 12, "chemical / agent", imp_name));
                            }
                        }
                    }
                }
            }


            bool IMPAlreadyThere(string imp_name)
            {
                bool res = false;
                foreach (StudyTopic t in topics)
                {
                    if (imp_name.ToLower() == t.original_value.ToLower())
                    {
                        res = true;
                        break;
                    }
                }
                return res;
            }


            var meddra_terms = r.Element("meddra_terms");
            if (meddra_terms != null)
            {
                var terms = meddra_terms.Elements("MeddraTerm");
                if (terms != null && terms.Count() > 0)
                {
                    foreach (XElement t in terms)
                    {
                        // MedDRA version and level details not used
                        // Term captured for possible MESH equivalence

                        string code = GetElementAsString(t.Element("code")) ?? "";
                        string term = GetElementAsString(t.Element("term")) ?? "";

                        if (term != "")
                        {
                            topics.Add(new StudyTopic(sid, 13, "condition", term, 16, code));
                        }
                    }
                }
            }


            var cs = r.Element("countries");
            if (cs != null)
            {
                var country_lines = cs.Elements("Country");
                if (country_lines?.Any() == true)
                {
                    foreach (XElement cline in country_lines)
                    {
                        string country_name = sh.ReplaceApos(GetElementAsString(cline.Element("name")).Trim());
                        string country_status = GetElementAsString(cline.Element("status"))?.Trim();

                        country_name = country_name.Replace("Korea, Republic of", "South Korea");
                        country_name = country_name.Replace("Russian Federation", "Russia");
                        country_name = country_name.Replace("Tanzania, United Republic of", "Tanzania");

                        if (string.IsNullOrEmpty(country_status))
                        {
                            countries.Add(new StudyCountry(sid, country_name));
                        }
                        else
                        {
                            int? status_id = string.IsNullOrEmpty(country_status) ? null : th.GetStatusId(country_status);
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
            SplitDate entered_in_db = dh.GetDatePartsFromISOString(GetElementAsString(r.Element("entered_in_db")));
            int? registry_pub_year = (entered_in_db != null) ? entered_in_db.year : s.study_start_year;

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
            string details_url = GetElementAsString(r.Element("details_url"));
            object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                        details_url, true, 35, "Web text"));


            // ----------------------------------------------------------
            // if there is a results url, add that in as well
            // ----------------------------------------------------------

            string results_url = GetElementAsString(r.Element("results_url"));
            if (!string.IsNullOrEmpty(results_url))
            {
                object_title = "EU CTR results entry";
                object_display_title = s.display_title + " :: EU CTR results entry";
                sd_oid = sid + " :: 28 :: " + object_title;

                // get the date data if available

                string results_first_date = GetElementAsString(r.Element("results_first_date"));
                string results_revision_date = GetElementAsString(r.Element("results_revision_date"));

                SplitDate results_date = dh.GetDatePartsFromEUCTRString(results_first_date);
                SplitDate results_revision = dh.GetDatePartsFromEUCTRString(results_revision_date);

                int? results_pub_year = results_date?.year;

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
                if (results_date != null)
                {
                    object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                             results_date.year, results_date.month, results_date.day, results_date.date_string));
                }

                if (results_revision != null)
                {
                    object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                         results_revision.year, results_revision.month, results_revision.day, results_revision.date_string));
                }

                // if there is a reference to a CSR pdf to download...
                // Seems to be on the web pages in two forms

                // Form A 

                string results_summary_link = GetElementAsString(r.Element("results_summary_link"));

                if (!string.IsNullOrEmpty(results_summary_link))
                {
                    string results_summary_name = GetElementAsString(r.Element("results_summary_name"));
                    int title_type_id = 0; string title_type = "";
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

                        data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_date.year,
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

                string results_pdf_link = GetElementAsString(r.Element("results_pdf_link"));

                if (!string.IsNullOrEmpty(results_pdf_link))
                {
                    object_title = "CSR summary - PDF DL";
                    object_display_title = s.display_title + " :: CSR summary";
                    int title_type_id = 22;
                    string title_type = "Study short name :: object type";

                    sd_oid = sid + " :: 79 :: " + object_title;

                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_date.year,
                          23, "Text", 79, "CSR summary", null, sponsor_name, 11, download_datetime));

                    // data object title is the single display title...
                    object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                                             title_type_id, title_type, true));

                    // instance url 
                    object_instances.Add(new ObjectInstance(sd_oid, 100123, "EU Clinical Trials Register",
                                         results_pdf_link, true, 11, "PDF"));

                }
            }


            // edit contributors - try to ensure properly categorised

            if (contributors.Count > 0)
            {
                foreach (StudyContributor sc in contributors)
                {
                    // all contributors originally down as organisations
                    // try and see if some are actually people

                    string orgname = sc.organisation_name.ToLower();
                    if (ih.CheckIfIndividual(orgname))
                    {
                        sc.person_full_name = sh.TidyPersonName(sc.organisation_name);
                        sc.organisation_name = null;
                        sc.is_individual = true;
                    }
                }
            }


            s.brief_description = study_description;

            s.identifiers = identifiers;
            s.titles = titles;
            s.contributors = contributors;
            s.topics = topics;
            s.features = features;
            s.countries = countries;

            s.data_objects = data_objects;
            s.object_titles = object_titles;
            s.object_instances = object_instances;
            s.object_dates = object_dates;


            return s;

        }
        {
            return null;
        }
    }
}

