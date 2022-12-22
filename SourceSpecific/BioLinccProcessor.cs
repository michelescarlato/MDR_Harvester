using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DataHarvester.biolincc
{
    public class BioLinccProcessor : IStudyProcessor
    { 
        IMonitorDataLayer _mon_repo;
        LoggingHelper _logger;

        public BioLinccProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger)
        {
            _mon_repo = mon_repo;
            _logger = logger;
        }

        public Study ProcessData(XmlDocument d, DateTime? download_datetime)
        {
            Study s = new Study();
            // get date retrieved in object fetch
            // transfer to study and data object records

            //List<StudyIdentifier> study_identifiers = new List<StudyIdentifier>();
            List<StudyTitle> study_titles = new List<StudyTitle>();
            List<StudyIdentifier> study_identifiers = new List<StudyIdentifier>();
            List<StudyReference> study_references = new List<StudyReference>();
            List<StudyContributor> study_contributors = new List<StudyContributor>();

            List<DataObject> data_objects = new List<DataObject>();
            List<ObjectDataset> object_datasets = new List<ObjectDataset>();
            List<ObjectTitle> data_object_titles = new List<ObjectTitle>();
            List<ObjectDate> data_object_dates = new List<ObjectDate>();
            List<ObjectInstance> data_object_instances = new List<ObjectInstance>();

            MD5Helpers hh = new MD5Helpers();
            DateHelpers dh = new DateHelpers();
            StringHelpers sh = new StringHelpers(_logger);

            char[] splitter = { '(' };

            // transfer features of main study object
            // In most cases study will have already been registered in CGT

            // First convert the XML document to a Linq XML Document.

            XDocument xDoc = XDocument.Load(new XmlNodeReader(d));

            // Obtain the main top level elements of the registry entry.

            XElement r = xDoc.Root;

            string sid = GetElementAsString(r.Element("sd_sid"));
            s.sd_sid = sid;
            s.datetime_of_data_fetch = download_datetime;

            // For the study, set up two titles, acronym and display title
            // NHLBI title not always exactly the same as the trial registry entry.

            string title = GetElementAsString(r.Element("title"));
            title = sh.ReplaceTags(title);
            title = sh.ReplaceApos(title);

            // study display title (= default title) always the biolincc one
            s.display_title = title;
            study_titles.Add(new StudyTitle(sid, title, 18, "Other scientific title", true, "From BioLINCC web page"));

            // but set up a 'name base' for data object names
            // which will be the CGT name if one exists as this is usually shorter
            // Only possible if the study is not one of those that are in a group,
            // collectively corresponding to a single NCT entry and public title, 
            // and only for those where an nct entry exists (Some BioLincc studiues are not registered)

            string nct_name = GetElementAsString(r.Element("nct_base_name"));

            // this statement temporarily required until nct names improved
            nct_name = sh.ReplaceApos(nct_name);

            string name_base = "";
            bool in_multiple_biolincc_group = GetElementAsBool(r.Element("in_multiple_biolincc_group"));
            if (!in_multiple_biolincc_group && !string.IsNullOrEmpty(nct_name))
            {
                name_base = nct_name;
            }
            else
            {
                name_base = title;
            }

            string acronym = GetElementAsString(r.Element("acronym"));
            if (!string.IsNullOrEmpty(acronym))
            {
                study_titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From BioLINCC web page"));
            }

            string brief_description = GetElementAsString(r.Element("brief_description"));
            s.brief_description = sh.StringClean(brief_description);


            s.study_type_id = GetElementAsInt(r.Element("study_type_id"));
            s.study_type = GetElementAsString(r.Element("study_type"));
            s.study_status_id = 21;
            s.study_status = "Completed";  // assumption for entry onto web site

            // Gender eligibility is never provided for biolincc entries

            s.study_gender_elig_id = 915;
            s.study_gender_elig = "Not provided";

            string study_period = GetElementAsString(r.Element("study_period"));
            study_period = study_period.Trim();
            if (study_period.Length > 3)
            {
                string first_four = study_period.Substring(0, 4);
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
            string hbli_identifier = GetElementAsString(r.Element("accession_number"));

            // identifier type = NHBLI ID, id = 42, org = National Heart, Lung, and Blood Institute, id = 100167.
            study_identifiers.Add(new StudyIdentifier(sid, hbli_identifier, 42, "NHLBI ID", 100167, "National Heart, Lung, and Blood Institute (US)"));

            // If there is a NCT ID (there usually is...).
            var registry_ids = r.Element("registry_ids");
            if (registry_ids != null)
            {
                var ids = registry_ids.Elements("RegistryId");
                if (ids != null && ids.Count() > 0)
                {
                    foreach (XElement id in ids)
                    {
                        string nct_id = GetElementAsString(id.Element("nct_id"));
                        study_identifiers.Add(new StudyIdentifier(sid, nct_id, 11, "Trial Registry ID", 100120, "ClinicalTrials.gov"));
                    }
                }
            }

            int? sponsor_id = GetElementAsInt(r.Element("sponsor_id"));
            string sponsor_name = GetElementAsString(r.Element("sponsor_name"));
            if (sponsor_id != null)
            {
                study_contributors.Add(new StudyContributor(sid, 54, "Trial sponsor", sponsor_id, sponsor_name));
            }

            // Create data object records.

            // For the BioLincc web page, set up new data object, object title, object_instance and object dates
            
            // Get publication year if one exists
            int? pub_year = null;
            int? pyear = GetElementAsInt(r.Element("publication_year"));
            if (pyear != null && pyear > 0)
            {
                pub_year = pyear;
            }

            string remote_url = GetElementAsString(r.Element("remote_url"));
            string object_title = "NHLBI web page";
            string object_display_title = name_base + " :: " + "NHLBI web page";

            // create hash Id for the data object
            string sd_oid = sid + " :: 38 :: " + object_title;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, pub_year, 23, "Text", 38, "Study Overview",
                100167, "National Heart, Lung, and Blood Institute (US)", 12, download_datetime));

            data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                                "Study short name :: object type", true));

            data_object_instances.Add(new ObjectInstance(sd_oid, 101900, "BioLINCC",
                                remote_url, true, 35, "Web text"));

            // Add dates if available
            //SplitDate page_prepared = null;
            SplitDate last_revised = null;

            /*
            // This date no longer available... (explicitly - last revised date may be available in header)
             //string page_prepared_date = GetElementAsString(r.Element("page_prepared_date"));


            if (!string.IsNullOrEmpty(page_prepared_date))
            {
                page_prepared = dh.GetDatePartsFromISOString(page_prepared_date.Substring(0, 10));
                data_object_dates.Add(new ObjectDate(sd_oid, 12, "Available", page_prepared.year,
                            page_prepared.month, page_prepared.day, page_prepared.date_string));
            }
            */

            string last_revised_date = GetElementAsString(r.Element("last_revised_date"));
            if (!string.IsNullOrEmpty(last_revised_date))
            {
                // date datasets revised
                last_revised = dh.GetDatePartsFromISOString(last_revised_date.Substring(0, 10));

                // only add last revised date if it is later than date prepared date
                // For early trials run before BioLINCC waas set up this is not the case
                /*
                if ((last_revised.year * 400) + (last_revised.month * 32) + last_revised.day
                    >= (page_prepared.year * 400) + (page_prepared.month * 32) + page_prepared.day)

                {
                    data_object_dates.Add(new ObjectDate(sd_oid, 18, "Updated", last_revised.year,
                              last_revised.month, last_revised.day, last_revised.date_string));
                }
                */
            }

            // If there is a study web site...
            string study_website = GetElementAsString(r.Element("study_website"));
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

            string resources_available = GetElementAsString(r.Element("resources_available"));
            if (resources_available.ToLower().Contains("datasets"))
            {
                DateTime date_access_url_checked = new DateTime(2021, 7, 23);

                object_title = "Individual participant data";
                object_display_title = name_base + " :: " + "Individual participant data";
                sd_oid = sid + " :: 80 :: " + object_title;

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, last_revised.year, 14, "Datasets",
                        80, "Individual participant data", 100167, "National Heart, Lung, and Blood Institute (US)",
                        17, "Case by case download", access_details,
                        "https://biolincc.nhlbi.nih.gov/media/guidelines/handbook.pdf?link_time=2019-12-13_11:33:44.807479#page=15",
                        date_access_url_checked, download_datetime));

                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22, "Study short name :: object type", true));
                data_object_dates.Add(new ObjectDate(sd_oid, 18, "Updated", last_revised.year, last_revised.month, last_revised.day, last_revised.date_string));

                // Datasets and consent restrictions
                string dataset_consent_restrictions = GetElementAsString(r.Element("dataset_consent_restrictions"));

                int consent_type_id = 0;
                string consent_type = null;
                string restrictions = null;
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
            }

            var primary_docs = r.Element("primary_docs");
            if (primary_docs != null)
            {
                var docs = primary_docs.Elements("PrimaryDoc");
                if (docs != null && docs.Count() > 0)
                {
                    foreach (XElement doc in docs)
                    {
                        string pubmed_id = GetElementAsString(doc.Element("pubmed_id"));
                        string url = GetElementAsString(doc.Element("url"));
                        study_references.Add(new StudyReference(sid, pubmed_id, null, url, "primary")); ;
                    }
                }
            }


            var resources = r.Element("resources");
            if (resources != null)
            {
                var docs = resources.Elements("Resource");
                if (docs != null && docs.Count() > 0)
                {
                    foreach (XElement doc in docs)
                    {
                        string doc_name = GetElementAsString(doc.Element("doc_name"));
                        int? object_type_id = GetElementAsInt(doc.Element("object_type_id"));
                        string object_type = GetElementAsString(doc.Element("object_type"));
                        int? access_type_id = GetElementAsInt(doc.Element("access_type_id"));
                        string url = GetElementAsString(doc.Element("url"));
                        int? doc_type_id = GetElementAsInt(doc.Element("doc_type_id"));
                        string doc_type = GetElementAsString(doc.Element("doc_type"));
                        string size = GetElementAsString(doc.Element("size"));
                        string size_units = GetElementAsString(doc.Element("size_units"));

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
            }


            var assoc_docs = r.Element("assoc_docs");
            if (assoc_docs != null)
            {
                var docs = assoc_docs.Elements("AssocDoc");
                if (docs != null && docs.Count() > 0)
                {
                    foreach (XElement doc in docs)
                    {
                        string pubmed_id = GetElementAsString(doc.Element("pubmed_id"));
                        string display_title = sh.ReplaceApos(GetElementAsString(doc.Element("display_title")));
                        string link_id = GetElementAsString(doc.Element("link_id"));
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


        private string GetElementAsString(XElement e) => (e == null) ? null : (string)e;

        private string GetAttributeAsString(XAttribute a) => (a == null) ? null : (string)a;


        private int? GetElementAsInt(XElement e)
        {
            string evalue = GetElementAsString(e);
            if (string.IsNullOrEmpty(evalue))
            {
                return null; 
            }
            else
            {
                if (Int32.TryParse(evalue, out int res))
                    return res;
                else
                    return null;
            }
        }

        private int? GetAttributeAsInt(XAttribute a)
        {
            string avalue = GetAttributeAsString(a);
            if (string.IsNullOrEmpty(avalue))
            {
                return null;
            }
            else
            {
                if (Int32.TryParse(avalue, out int res))
                    return res;
                else
                    return null;
            }
        }


        private bool GetElementAsBool(XElement e)
        {
            string evalue = GetElementAsString(e);
            if (evalue != null)
            {
                return (evalue.ToLower() == "true" || evalue.ToLower()[0] == 'y') ? true : false;
            }
            else
            {
                return false;
            }
        }

        private bool GetAttributeAsBool(XAttribute a)
        {
            string avalue = GetAttributeAsString(a);
            if (avalue != null)
            {
                return (avalue.ToLower() == "true" || avalue.ToLower()[0] == 'y') ? true : false;
            }
            else
            {
                return false;
            }
        }
    }
}
