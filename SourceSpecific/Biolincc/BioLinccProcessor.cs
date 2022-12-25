using MDR_Harvester.Extensions;
using System.Text.Json;


namespace MDR_Harvester.Biolincc;

public class BioLinccProcessor : IStudyProcessor
{
    IMonitorDataLayer _mon_repo;
    LoggingHelper _logger;

    public BioLinccProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger)
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

        BioLincc_Record? b = JsonSerializer.Deserialize<BioLincc_Record?>(json_string, json_options);
        if (b is not null)
        {
            Study s = new Study();

            // get date retrieved in object fetch
            // transfer to study and data object records

            List<StudyTitle> study_titles = new();
            List<StudyIdentifier> study_identifiers = new();
            List<StudyReference> study_references = new();
            List<StudyContributor> study_contributors = new();
            List<StudyRelationship> study_relationships = new();

            List<DataObject> data_objects = new();
            List<ObjectDataset> object_datasets = new();
            List<ObjectTitle> data_object_titles = new();
            List<ObjectDate> data_object_dates = new();
            List<ObjectInstance> data_object_instances = new();
            
            MD5Helpers hh = new();

            // transfer features of main study object
            // In most cases study will have already been registered in CGT

            string sid = b.sd_sid!;
            s.sd_sid = sid;
            s.datetime_of_data_fetch = download_datetime;

            // For the study, set up two titles, acronym and display title
            // NHLBI title not always exactly the same as the trial registry entry.

            string? title = s.display_title;
            if (title is not null)
            {
                title = title.ReplaceTags()?.ReplaceApos();
            }

            // study display title (= default title) always the biolincc one.

            s.display_title = title;
            study_titles.Add(new StudyTitle(sid, title, 18, "Other scientific title", true, "From BioLINCC web page"));

            // but set up a 'name base' for data object names
            // which will be the CGT name if one exists as this is usually shorter
            // Only possible if the study is not one of those that are in a group,
            // collectively corresponding to a single NCT entry and public title, 
            // and only for those where an nct entry exists (Some BioLincc studiues are not registered)

            string? nct_name = b.nct_base_name?.ReplaceApos();
            string? name_base = "";
            bool in_multiple_biolincc_group = b.in_multiple_biolincc_group is null ? false : (bool)b.in_multiple_biolincc_group;
            name_base = (!in_multiple_biolincc_group && !string.IsNullOrEmpty(nct_name)) ? nct_name : title;

            string? acronym = b.acronym;
            if (!string.IsNullOrEmpty(acronym))
            {
                study_titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From BioLINCC web page"));
            }

            s.brief_description = b.brief_description?.StringClean();
            s.study_type_id = b.study_type_id;
            s.study_type = b.study_type;
            s.study_status_id = 21;
            s.study_status = "Completed";  // assumption for entry onto web site

            // Gender eligibility is never provided for biolincc entries

            s.study_gender_elig_id = 915;
            s.study_gender_elig = "Not provided";

            string? study_period = b.study_period?.Trim();
            if (study_period is not null && study_period.Length > 3)
            {
                string first_four = study_period[..4];
                if (first_four == first_four.Trim())
                {
                    if (Int32.TryParse(first_four, out int start_year))
                    {
                        s.study_start_year = start_year;
                    }
                    else
                    {
                        // perhaps full month year - e.g. "December 2008..."
                        // Get first word
                        // Is it a month name? - if so, store the number 
                        if (study_period.IndexOf(" ") != -1)
                        {
                            int spacepos = study_period.IndexOf(" ");
                            string month_name = study_period.Substring(0, spacepos);
                            if (Enum.TryParse<MonthsFull>(month_name, out MonthsFull month_enum))
                            {
                                // get value...
                                int start_month = (int)month_enum;

                                // ...and get next 4 characters - are they a year?
                                // if they are it is the start year
                                string next_four = study_period.Substring(spacepos + 1, 4);
                                if (Int32.TryParse(next_four, out start_year))
                                {
                                    s.study_start_month = start_month;
                                    s.study_start_year = start_year;
                                }
                            }
                        }
                    }
                }
            }

            // Add study attribute records.
            string? hbli_identifier = b.accession_number;
            if (hbli_identifier is not null)
            {
                // identifier type = NHBLI ID, id = 42, org = National Heart, Lung, and Blood Institute, id = 100167.
                study_identifiers.Add(new StudyIdentifier(sid, hbli_identifier, 42, "NHLBI ID", 100167, "National Heart, Lung, and Blood Institute (US)"));
            }

            // If there is a NCT ID (there usually is...).
            var registry_ids = b.registry_ids;
            if (registry_ids?.Any() == true)
            {
                foreach (var rid in registry_ids)
                {
                    string? nct_id = rid.nct_id;
                    if (nct_id is not null)
                    {
                        study_identifiers.Add(new StudyIdentifier(sid, nct_id, 11, "Trial Registry ID", 100120, "ClinicalTrials.gov"));
                    }
                }

            }

            int? sponsor_id = b.sponsor_id;
            string? sponsor_name = b.sponsor_name;
            if (sponsor_id is not null)
            {
                study_contributors.Add(new StudyContributor(sid, 54, "Trial sponsor", sponsor_id, sponsor_name));
            }


            var rel_studies = b.related_studies;
            if (rel_studies?.Any() == true)
            {
                foreach (var relstudy in rel_studies)
                {
                    string? related_study = relstudy.link_text;
                    string? study_link = relstudy.study_link;

                    // relationshiup is simply 'is related' as no further information is provided
                    study_relationships.Add(new StudyRelationship(sid, 27,
                                     "Has link listed in registry but nature of link unclear", related_study));
                }
            }


            // Create data object records.

            // For the BioLincc web page, set up new data object, object title, object_instance and object dates

            // Get publication year if one exists
            int? pub_year = null;
            int? pyear = b.publication_year;
            if (pyear != null && pyear > 0)
            {
                pub_year = pyear;
            }

            string? remote_url = b.remote_url;
            string? object_title = "NHLBI web page";
            string? object_display_title = name_base + " :: " + "NHLBI web page";

            // create Id for the data object
            string sd_oid = sid + " :: 38 :: " + object_title;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, pub_year, 23, "Text", 38, "Study Overview",
                100167, "National Heart, Lung, and Blood Institute (US)", 12, download_datetime));

            data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                "Study short name :: object type", true));

            data_object_instances.Add(new ObjectInstance(sd_oid, 101900, "BioLINCC",
                                remote_url, true, 35, "Web text"));

            // Add dates if available.
            // generally now, page prepared appears to be null;
            // last revised refers to last date datasets revised.

            DateTime? page_prepared_date = b.page_prepared_date;
            DateTime? last_revised_date = b.datasets_updated_date;
            if (page_prepared_date is not null)
            {
                DateTime dt = (DateTime)page_prepared_date;
                data_object_dates.Add(new ObjectDate(sd_oid, 12, "Available", dt.Year,
                            dt.Month, dt.Day, dt.ToString("yyyy MMM dd")));
            }


            // If there is a study web site...
            string? study_website = b.study_website;
            if (!string.IsNullOrEmpty(study_website))
            {
                object_title = "Study web site";
                object_display_title = name_base + " :: " + "Study web site";
                sd_oid = sid + " :: 134 :: " + object_title;

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null, 23, "Text", 134, "Website",
                                    sponsor_id, sponsor_name, 12, download_datetime));
                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                    "Study short name :: object type", true));
                data_object_instances.Add(new ObjectInstance(sd_oid, sponsor_id, sponsor_name,
                                    study_website, true, 35, "Web text"));
            }


            // create the data object relating to the dataset, instance not available, title possible...
            // may be a description of the data in 'Data Available...'
            // if so add a data object description....with a data object title

            string access_details = "Investigators wishing to request materials from studies ... must register (free) on the BioLINCC website. ";
            access_details += "Registered investigators may then request detailed searches and submit an application for data sets ";
            access_details += "and/or biospecimens. (from the BioLINCC website)";

            string de_identification = "All BioLINCC data and biospecimens are de-identified. Obvious subject identifiers ";
            de_identification += "and data collected solely for administrative purposes are redacted from datasets, ";
            de_identification += "and dates are recoded relative to a specific reference point. ";
            de_identification += "In addition recodes of selected low-frequency data values may be ";
            de_identification += "carried out to protect subject privacy and minimize re-identification risks (from the BioLINCC documentation).";

            string? resources_available = b.resources_available;
            if (resources_available is not null && resources_available.ToLower().Contains("datasets"))
            {
                DateTime date_access_url_checked = new DateTime(2021, 7, 23);

                object_title = "Individual participant data";
                object_display_title = name_base + " :: " + "Individual participant data";
                sd_oid = sid + " :: 80 :: " + object_title;
                int? dataset_year = last_revised_date is null ? null : ((DateTime)last_revised_date).Year;

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, dataset_year, 14, "Datasets",
                        80, "Individual participant data", 100167, "National Heart, Lung, and Blood Institute (US)",
                        17, "Case by case download", access_details,
                        "https://biolincc.nhlbi.nih.gov/media/guidelines/handbook.pdf?link_time=2019-12-13_11:33:44.807479#page=15",
                        date_access_url_checked, download_datetime));

                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22, "Study short name :: object type", true));

                if (last_revised_date is not null)
                {
                    DateTime dt = (DateTime)last_revised_date;
                    data_object_dates.Add(new ObjectDate(sd_oid, 18, "Updated", dt.Year,
                                dt.Month, dt.Day, dt.ToString("yyyy MMM dd")));
                }
                // Datasets and consent restrictions
                string? dataset_consent_restrictions = b.dataset_consent_restrictions;

                int consent_type_id = 0;
                string? consent_type = null;
                string? restrictions = null;
                if (string.IsNullOrEmpty(dataset_consent_restrictions))
                {
                    consent_type_id = 0;
                    consent_type = "Not known";
                }
                else if (dataset_consent_restrictions.ToLower() == "none"
                    || dataset_consent_restrictions.ToLower() == "none.")
                {
                    consent_type_id = 2;
                    consent_type = "No restriction";
                    restrictions = "Explicitly states that there are no restrictions on use";
                }
                else
                {
                    consent_type_id = 6;
                    consent_type = "Consent specified, not elsewhere categorised";
                    restrictions = dataset_consent_restrictions;
                }

                // do dataset object separately
                object_datasets.Add(new ObjectDataset(sd_oid,
                                         0, "Not known", null,
                                         2, "De-identification applied", de_identification,
                                         consent_type_id, consent_type, restrictions));

                if (last_revised_date is not null)
                {
                    DateTime dt = (DateTime)last_revised_date;
                    data_object_dates.Add(new ObjectDate(sd_oid, 18, "Updated", dt.Year,
                                dt.Month, dt.Day, dt.ToString("yyyy MMM dd")));
                }
            }

            var primary_docs = b.primary_docs;
            if (primary_docs?.Any() == true)
            {
                foreach (var doc in primary_docs)
                {
                    string? pubmed_id = doc.pubmed_id;
                    string? url = doc.url;
                    if (url is not null)
                    {
                        study_references.Add(new StudyReference(sid, pubmed_id, null, url, "primary"));
                    }
                }
            }

            var resources = b.resources;
            if (resources is not null)
            {
                foreach (var res in resources)
                {
                    string? doc_name = res.doc_name;
                    int? object_type_id = res.object_type_id;
                    string? object_type = res.object_type;
                    int? access_type_id = res.access_type_id;
                    string? url = res.url;
                    int? doc_type_id = res.doc_type_id;
                    string? doc_type = res.doc_type;
                    string? size = res.size;
                    string? size_units = res.size_units;

                    // for parity and test expectations
                    if (size == "") size = null;
                    if (size_units == "") size_units = null;

                    object_title = doc_name;
                    object_display_title = name_base + " :: " + doc_name;
                    sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                    // N.B. 'pub_year' no longer known

                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null, 23, "Text", object_type_id, object_type,
                                    100167, "National Heart, Lung, and Blood Institute (US)", access_type_id, download_datetime));
                    data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 21, "Study short name :: object name", true));
                    data_object_instances.Add(new ObjectInstance(sd_oid, 101900, "BioLINCC", url, true, doc_type_id, doc_type, size, size_units));
                }
            }


            var assoc_docs = b.assoc_docs;
            if (assoc_docs?.Any() == true)
            {
                foreach (var doc in assoc_docs)
                {
                    string? pubmed_id = doc.pubmed_id;
                    string? display_title = doc.display_title?.ReplaceApos();
                    string? link_id = doc.link_id;
                    if (display_title is not null)
                    {
                        study_references.Add(new StudyReference(s.sd_sid, pubmed_id, display_title, link_id, "associated"));
                    }
                }
            }


            // check that the primary doc is not duplicated in the associated docs (it sometimes is)
            if (study_references.Count > 0)
            {
                foreach (StudyReference p in study_references)
                {
                    if (p.comments == "primary")
                    {
                        foreach (StudyReference a in study_references)
                        {
                            if (a.comments == "associated" && p.pmid == a.pmid)
                            {
                                // update the primary link
                                p.citation = a.citation;
                                p.doi = a.doi;
                                // drop the redundant associated link
                                a.comments = "to go";
                                break;
                            }
                        }
                    }
                }
            }

            List<StudyReference> study_references2 = new List<StudyReference>();
            foreach (StudyReference a in study_references)
            {
                if (a.comments != "to go")
                {
                    study_references2.Add(a);
                }
            }


            // add in the study properties
            s.titles = study_titles;
            s.identifiers = study_identifiers;
            s.references = study_references2;
            s.contributors = study_contributors;

            s.data_objects = data_objects;
            s.object_datasets = object_datasets;
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