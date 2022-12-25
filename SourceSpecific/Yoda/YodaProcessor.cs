using MDR_Harvester.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;


namespace MDR_Harvester.Yoda; 

public class YodaProcessor : IStudyProcessor
{
    private readonly LoggingHelper _logger;

    public YodaProcessor(LoggingHelper logger)
    {
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

        Yoda_Record? b = JsonSerializer.Deserialize<Yoda_Record?>(json_string, json_options);
        if (b is not null)
        {
            Study s = new();

            // get date retrieved in object fetch
            // transfer to study and data object records

            List<StudyIdentifier> study_identifiers = new();
            List<StudyTitle> study_titles = new();
            List<StudyReference> study_references = new();
            List<StudyContributor> study_contributors = new();
            List<StudyTopic> study_topics = new();

            List<DataObject> data_objects = new();
            List<ObjectDataset> object_datasets = new();
            List<ObjectTitle> data_object_titles = new();
            List<ObjectInstance> data_object_instances = new();

            string sid = b.sd_sid!;
            s.sd_sid = sid;
            s.datetime_of_data_fetch = download_datetime;

            string? yoda_title = b.yoda_title;  
            yoda_title = yoda_title.ReplaceApos()?.ReplaceTags();
            s.display_title = yoda_title;

            // name_base derived from CTG during download, if possible.
            // In most cases the name_base will be the NCT title.

            string? name_base_title = b.name_base_title?.ReplaceApos();  
            string? name_base = string.IsNullOrEmpty(name_base_title) ? yoda_title : name_base_title;

            var st_titles = b.study_titles;  
            if (st_titles?.Any() == true)
            {
                foreach (var t in st_titles)
                {
                    string? title_text = t.title_text;
                    int? title_type_id = t.title_type_id; 
                    string? title_type = t.title_type; 
                    bool? is_default = t.is_default;
                    string? comments = t.comments;
                    study_titles.Add(new StudyTitle(sid, title_text, title_type_id, title_type, is_default, comments));
                }
            }

            // brief description mostly as derived from CTG.

            s.brief_description = b.brief_description?.StringClean();
            s.study_status_id = 21;
            s.study_status = "Completed";  // assumption for entry onto web site

            // study type only really relevant for non registered studies (others will  
            // have type identified in registered study entry
            // here, usually previously obtained from the ctg or isrctn entry
            s.study_type_id = b.study_type_id;
            s.study_type = s.study_type_id switch
            {
                11 => "interventional",
                12 => "observational",
                13 => "observational patient registry",
                14 => "expanded access",
                15 => "funded programme",
                16 => "not yet known",
                _ => "not yet known"
            };

            s.study_enrolment = b.enrolment;
            string? percent_female = b.percent_female;
            if (!string.IsNullOrEmpty(percent_female) && percent_female != "N/A")
            {
                if (percent_female.EndsWith("%"))
                {
                    percent_female = percent_female[..^1];
                }

                if (Single.TryParse(percent_female, out float female_percentage))
                {
                    if (female_percentage == 0)
                    {
                        s.study_gender_elig_id = 910;
                        s.study_gender_elig = "Male";
                    }
                    else if (female_percentage == 100)
                    {
                        s.study_gender_elig_id = 905;
                        s.study_gender_elig = "Female";
                    }
                    else
                    {
                        s.study_gender_elig_id = 900;
                        s.study_gender_elig = "All";
                    }
                }
            }
            else
            {
                s.study_gender_elig_id = 915;
                s.study_gender_elig = "Not provided";
            }

            // transfer identifier data
            // Normally a protocol id will be the only addition (may be a duplicate of one already in the system).

            var study_idents = b.study_identifiers;  
            if (study_idents?.Any() == true)
            {
                foreach (var i in study_idents)
                {
                    string? identifier_value = i.identifier_value; 
                    int? identifier_type_id = i.identifier_type_id; 
                    string? identifier_type = i.identifier_type;
                    int? identifier_org_id = i.identifier_org_id;  
                    string? identifier_org = i.identifier_org?.ReplaceApos();

                    study_identifiers.Add(new StudyIdentifier(sid, identifier_value, identifier_type_id, identifier_type,
                                                        identifier_org_id, identifier_org));
                }
            }

            // study contributors
            // only sponsor knowm, and only relevant for non registered studies (others will  
            // have the sponsor identified in registered study entry).
            // If study registered elsewhere sponsor details wil be ignored during the aggregation.

            int? sponsor_org_id; string? sponsor_org;
            int? sponsor_id = b.sponsor_id; 
            string? sponsor = b.sponsor;
            if (!string.IsNullOrEmpty(sponsor))
            {
                sponsor_org_id = sponsor_id;
                sponsor_org = sponsor.TidyOrgName(sid);
            }
            else
            {
                sponsor_org_id = null;
                sponsor_org = "No organisation name provided in source data";
            }
            study_contributors.Add(new StudyContributor(sid, 54, "Study Sponsor", sponsor_org_id, sponsor_org));

            // study topics.

            string? compound_generic_name = b.compound_generic_name;
            string? compound_product_name = b.compound_product_name;
            if (!string.IsNullOrEmpty(compound_generic_name))
            {
                study_topics.Add(new StudyTopic(sid, 12, "chemical / agent", compound_generic_name));
            }

            if (!string.IsNullOrEmpty(compound_product_name))
            {
                string? product_name = compound_product_name.Replace(((char)174).ToString(), "");    // drop reg mark
                product_name = product_name?.CompressSpaces();

                // see if already exists
                bool add_product = true;
                foreach (StudyTopic t in study_topics)
                {
                    if (product_name.ToLower() == t.original_value?.ToLower())
                    {
                        add_product = false;
                        break;
                    }
                }

                if (add_product)
                {
                    product_name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(product_name.ToLower());
                    study_topics.Add(new StudyTopic(sid, 12, "chemical / agent", product_name));
                }
            }

            string? conditions_studied = b.conditions_studied;
            if (!string.IsNullOrEmpty(conditions_studied))
            {
                study_topics.Add(new StudyTopic(sid, 13, "condition", conditions_studied));
            }

            // create study references (pmids)
            var refs = b.study_references;
            if (refs?.Any() == true)
            {
                foreach (var sr in refs)
                {
                    string? pmid = sr.pmid;
                    string? link = sr.link;
                    
                    // normally only 1 if there is one there at all 
                    study_references.Add(new StudyReference(sid, pmid, link, null, null));
                }
            }

            // data objects...

            // do the yoda web page itself first...
            string object_title = "Yoda web page";
            string object_display_title = name_base + " :: " + "Yoda web page";
            string? remote_url = b.remote_url;

            // create hash Id for the data object
            string sd_oid = sid + " :: 38 :: " + object_title;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null, 23, "Text", 38, "Study Overview",
                              101901, "Yoda", 12, download_datetime));
            data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22,
                            "Study short name :: object type", true));
            data_object_instances.Add(new ObjectInstance(sd_oid, 101901, "Yoda",
                                remote_url, true, 35, "Web text"));

            // then for each supp doc...
            var sds = b.supp_docs;
            if (sds?.Any() == true)
            {
                foreach (var sd in sds)
                {
                    // get object_type
                    int object_class_id = 0; string object_class = "";
                    int object_type_id = 0; string object_type = "";
                    string? doc_name = sd.doc_name;
                    string? comment = sd.comment;
                    string? url = sd.url;
                    //object_title = doc_name;

                    if (doc_name is not null)
                    {
                        if (doc_name.Contains("Datasets"))
                        {
                            object_class_id = 14; object_class = "Datasets";
                        }
                        else
                        {
                            object_class_id = 23; object_class = "Text";
                        }

                        Tuple<int, string>? obtype = doc_name switch
                        {
                            "Collected Datasets" => new Tuple<int, string>(80, "Individual participant data"),
                            "Analysis Datasets" => new Tuple<int, string>(51, "IPD final analysis datasets (full study population)"),
                            "Data Definition Specification" => new Tuple<int, string>(31, "Data dictionary"),
                            "CSR Summary" => new Tuple<int, string>(79, "CSR summary"),
                            "Annotated Case Report Form" => new Tuple<int, string>(30, "Annotated data collection forms"),
                            "Protocol with Amendments" => new Tuple<int, string>(11, "Study protocol"),
                            "Clinical Study Report" => new Tuple<int, string>(26, "Clinical study report"),
                            "Statistical Analysis Plan" => new Tuple<int, string>(22, "Statistical analysis plan"),
                            _ => null
                        };

                        if (obtype is not null)
                        {
                            object_type_id = obtype.Item1;
                            object_type = obtype.Item2;

                            object_display_title = name_base + " :: " + object_type;
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                            if (comment == "Available now")
                            {
                                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null, object_class_id, object_class, object_type_id, object_type,
                                                101901, "Yoda", 11, download_datetime));
                                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22, "Study short name :: object type", true));

                                // create instance as resource exists
                                // get file type from link if possible
                                int resource_type_id = 0; string resource_type = "";
                                if (url.ToLower().EndsWith(".pdf"))
                                {
                                    resource_type_id = 11;
                                    resource_type = "PDF";
                                }
                                else if (url.ToLower().EndsWith(".xls"))
                                {
                                    resource_type_id = 18;
                                    resource_type = "Excel Spreadsheet(s)";
                                }
                                else
                                {
                                    resource_type_id = 0;
                                    resource_type = "Not yet known";
                                }
                                data_object_instances.Add(new ObjectInstance(sd_oid, 101901, "Yoda", url, true, resource_type_id, resource_type));
                            }
                            else
                            {
                                string access_details = "The YODA Project will require that requestors provide basic information about the Principal Investigator, Key Personnel, and the ";
                                access_details += "project Research Proposal, including a scientific abstract and research methods.The YODA Project will review proposals to ensure that: ";
                                access_details += "1) the scientific purpose is clearly described; 2) the data requested will be used to enhance scientific and/or medical knowledge; and ";
                                access_details += "3) the proposed research can be reasonably addressed using the requested data.";

                                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null, object_class_id, object_class, object_type_id, object_type,
                                                101901, "Yoda", 17, "Case by case download", access_details,
                                                "https://yoda.yale.edu/how-request-data", null, download_datetime));
                                data_object_titles.Add(new ObjectTitle(sd_oid, object_display_title, 22, "Study short name :: object type", true));
                            }

                            // for datasets also add dataset properties - even if they are largely unknown
                            if (object_type_id == 80)
                            {
                                object_datasets.Add(new ObjectDataset(sd_oid, 0, "Not known", null,
                                                            2, "De-identification applied",
                                                            "Yoda states that “...researchers will be granted access to participant-level study data that are devoid of personally identifiable information; current best guidelines for de-identification of data will be used.”",
                                                            0, "Not known", null));
                            }
                        }
                    }
                }
            }


            // add in the study properties
            s.identifiers = study_identifiers;
            s.titles = study_titles;
            s.references = study_references;
            s.contributors = study_contributors;
            s.topics = study_topics;

            s.data_objects = data_objects;
            s.object_datasets = object_datasets;
            s.object_titles = data_object_titles;
            s.object_instances = data_object_instances;

            return s;
        }
        else
        {
            return null;
        }
    }
}








