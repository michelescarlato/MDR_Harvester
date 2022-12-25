using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace MDR_Harvester.Pubmed;

public class PubmedProcessor : IObjectProcessor
{
    IMonitorDataLayer _mon_repo;
    LoggingHelper _logger;

    public PubmedProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger)
    {
        _mon_repo = mon_repo;
        _logger = logger;
    }

   
    public FullDataObject ProcessData(XmlDocument d, DateTime? download_datetime)
    {
        DateHelpers dh = new DateHelpers();
        TypeHelpers th = new TypeHelpers();
        IdentifierHelpers ih = new IdentifierHelpers();

        // First convert the XML document to a Linq XML Document.

        XDocument xDoc = XDocument.Load(new XmlNodeReader(d));

        // Obtain the main top level elements of the citation.

        XElement pubmedArticle = xDoc.Root;
        XElement citation = pubmedArticle.Element("MedlineCitation");
        XElement pubmed = pubmedArticle.Element("PubmedData");
        XElement article = citation.Element("Article");
        XElement journal = article.Element("Journal");
        XElement JournalInfo = citation.Element("MedlineJournalInfo");

        // Establish main citation object
        // and list structures to receive data

        List<ObjectInstance> instances = new List<ObjectInstance>();
        List<ObjectDate> dates = new List<ObjectDate>();
        List<ObjectTitle> titles = new List<ObjectTitle>();
        List<ObjectIdentifier> identifiers = new List<ObjectIdentifier>();
        List<ObjectTopic> topics = new List<ObjectTopic>();
        List<ObjectPublicationType> pubtypes = new List<ObjectPublicationType>();
        List<ObjectDescription> descriptions = new List<ObjectDescription>();
        List<ObjectContributor> contributors = new List<ObjectContributor>();
        List<ObjectComment> comments = new List<ObjectComment>();
        List<ObjectDBLink> db_ids = new List<ObjectDBLink>();

        List<string> language_list = new List<string>();
        string author_string = "";
        string art_title = "";
        string journal_title = "";
        string journal_source = "";


        #region Header

        // Identify the PMID as the source data object Id (sd_oid), and also construct and add 
        // this to the 'other identifiers' list ('other' because it is not a doi).
        // The date applied may or may not be available later.

        string sdoid = GetElementAsString(citation.Element("PMID"));

        FullDataObject fob = new FullDataObject(sdoid, download_datetime);

        // add in the defaults for pubmed articles
        fob.object_class_id = 23;
        fob.object_class = "Text";
        fob.object_type_id = 12;
        fob.object_type = "Journal Article";
        fob.add_study_contribs = false;
        fob.add_study_topics = false;
        fob.eosc_category = 0;

        identifiers.Add(new ObjectIdentifier(sdoid, 16, "PMID", sdoid, 100133, "National Library of Medicine"));

        // Set the PMID entry as an object instance 
        // (type id 3 = abstract, resource 40 = Web text journal abstract), add to the instances list.
       
        instances.Add(new ObjectInstance(sdoid, 3, "Article abstract", 100133, "National Library of Medicine",
               "https://www.ncbi.nlm.nih.gov/pubmed/" + sdoid, true,
                40, "Web text journal abstract"));


        // Can assume there is always a PMID element ... (if not the 
        // original data search for this citation would not have worked).
        // Get the associated version and note if it is not present or 
        // not in the right format - 
        // these exceptions appear to be very rare if they occur at all.


        var p = citation.Element("PMID");
        string pmidVersion = GetAttributeAsString(p.Attribute("Version"));
        if (pmidVersion != null)
        {
            if (Int32.TryParse(pmidVersion, out int res))
            {
                fob.version = res.ToString();
            }
            else
            {
                fob.version = pmidVersion;
                _logger.LogLine("PMID version for {sdoid} not an integer", sdoid);
            }
        }
        else
        {
            _logger.LogLine("No PMID version attribute found for {sdoid}", sdoid);
        }


        // Obtain and store the citation status.

        string abstract_status = GetAttributeAsString(citation.Attribute("Status"));

        // Version and version_date hardly ever present
        // if they do occur log them.

        string version_id = GetAttributeAsString(citation.Attribute("VersionID"));
        string version_date = GetAttributeAsString(citation.Attribute("VersionDate"));
        if (version_id != null)
        {
            string qText = "A version attribute (" + version_id + ") found for pmid {sdoid}";
            _logger.LogLine(qText, sdoid);
        }
        if (version_date != null)
        {
            string qText = "A version date attribute (" + version_date + ") found for pmid {sdoid}";
            _logger.LogLine(qText, sdoid);
        }

        #endregion



        #region Basic Properties

        // Obtain and store the article publication model

        string pub_model = GetAttributeAsString(article.Attribute("PubModel"));

        // Obtain and store (in the languages list) the article's language(s) - 
        // get these early as may be needed by title extraction code below.

        var languages = article.Elements("Language");
        if (languages.Count() > 0)
        {
            string lang_list = "";
            foreach (string g in languages)
            {
                string lang_2code;
                if (g == "eng")
                {
                    lang_2code = "en";
                }
                else
                {
                    lang_2code = sh.lang_3_to_2(g);
                    if (lang_2code == "??")
                    {
                        // need to use the database
                        lang_2code = _mon_repo.lang_3_to_2(g);
                    }
                }
                language_list.Add(lang_2code);
                lang_list += ", " + lang_2code;
            }
            fob.lang_code = lang_list.Substring(2);
        }


        // Obtain article title(s).
        // Usually just a single title present in English, but may be an additional
        // title in the 'vernacular', with a translation in English.
        // Translated titles are in square brackets and may be followed by a comment
        // in parantheses. 
        // First set up the set of required variables.

        bool article_title_present = true;
        bool vernacular_title_present = false;
        string atitle = "";
        string vtitle = "";
        string vlang_code = "";

        // get some basic journal information, as this is useful for helping to 
        // determine the country of origin, and for identifying the publisher,
        // as well as later (in creating citation string). The journal name
        // and issn numbers for electronic and / or paper versions are obtained.

        if (journal != null)
        {
            JournalDetails jd = new JournalDetails(sdoid);
            jd.journal_title = GetElementAsString(journal.Element("Title"));

            IEnumerable<XElement> ISSNs = journal.Elements("ISSN");
            if (ISSNs.Count() > 0)
            {
                foreach (XElement issn_id in ISSNs)
                {
                    // Note the need to clean pissn / eissn numbers to a standard format.

                    string ISSN_type = GetAttributeAsString(issn_id.Attribute("IssnType"));
                    if (ISSN_type == "Print")
                    {
                        string pissn = GetElementAsString(issn_id);
                        if (pissn.Length == 9 && pissn[4] == '-')
                        {
                            pissn = pissn.Substring(0, 4) + pissn.Substring(5, 4);
                        }
                        jd.pissn = pissn;
                    }
                    if (ISSN_type == "Electronic")
                    {
                        string eissn = GetElementAsString(issn_id);
                        if (eissn.Length == 9 && eissn[4] == '-')
                        {
                            eissn = eissn.Substring(0, 4) + eissn.Substring(5, 4);
                        }
                        jd.eissn = eissn;
                    }
                }
            }

            fob.journal_details = jd;
        }


        // Get the main article title and check for any html. Log any exception conditions.
        // Can't use the standard helper methods here as these strip out contained html,
        // therefore use an XML reader to obtain the InnerXML of the Title element.

        XElement article_title = article.Element("ArticleTitle");
        if (article_title != null)
        {
            var areader = article_title.CreateReader();
            areader.MoveToContent();
            atitle = areader.ReadInnerXml().Trim();
            if (atitle != "")
            {
               atitle = sh.ReplaceTags(atitle);
               atitle = sh.ReplaceApos(atitle);
            }
            else
            {
                article_title_present = false;
                string qText = "The citation has an empty article title element, pmid {sdoid}";
                _logger.LogLine(qText, sdoid);
            }
       }
       else
       {
           article_title_present = false;
           string qText = "The citation does not have an article title element, pmid {sdoid}";
            _logger.LogLine(qText, sdoid);
        }


        // Get the vernacular title if there is one and characterise it
        // in a similar way, noting any html.

        XElement vernacular_title = article.Element("VernacularTitle");

        if (vernacular_title != null)
        {
            vernacular_title_present = true;
            var vreader = vernacular_title.CreateReader();
            vreader.MoveToContent();
            vtitle = vreader.ReadInnerXml().Trim();

            if (vtitle != "")
            {
                vtitle = sh.ReplaceTags(vtitle);
                vtitle = sh.ReplaceApos(vtitle);

                // Try and get vernacular code language - not explicitly given so
                // all methods imperfect but seem to work in most situations so far.

                // Find first, if any, of non english in language list

                foreach (string s in language_list)
                {
                    if (s != "en")
                    {
                        vlang_code = s;
                        break;
                    }
                }

                if (vlang_code == "")
                {
                    // Check journal country of publication - suggests a reasonable guess!

                    if (JournalInfo != null)
                    {
                        string country = GetElementAsString(JournalInfo.Element("Country"));
                        switch (country)
                        {
                            case "Canada": vlang_code = "fr"; break;
                            case "France": vlang_code = "fr"; break;
                            case "Germany": vlang_code = "de"; break;
                            case "Spain": vlang_code = "es"; break;
                            case "Mexico": vlang_code = "es"; break;
                            case "Argentina": vlang_code = "es"; break;
                            case "Chile": vlang_code = "es"; break;
                            case "Peru": vlang_code = "es"; break;
                            case "Portugal": vlang_code = "pt"; break;
                            case "Brazil": vlang_code = "pt"; break;
                            case "Italy": vlang_code = "it"; break;
                            case "Russia": vlang_code = "ru"; break;
                            case "Turkey": vlang_code = "tr"; break;
                            case "Hungary": vlang_code = "hu"; break;
                            case "Poland": vlang_code = "pl"; break;
                            case "Sweden": vlang_code = "sv"; break;
                            case "Norway": vlang_code = "no"; break;
                            case "Denmark": vlang_code = "da"; break;
                            case "Finland": vlang_code = "fi"; break;
                            // may need to add more...
                        }
                    }
                }

                if (vlang_code == "")
                {
                    // If still blank, some Canadian journals are published in the US
                    // and often have a French alternate title.

                    if (journal_title.Contains("Canada") || journal_title.Contains("Canadian"))
                    {
                        vlang_code = "fr";
                    }

                }

                // But check the vernaculat title is not the same as the article title - can happen 
                // very rarely and if it is the case the vernacular title should be ignored.

                if (vtitle == atitle)
                {
                    vernacular_title_present = false;
                    string qText = "The article and vernacular titles seem identical, for pmid {sdoid}";
                    _logger.LogLine(qText, sdoid);
                }
            }
        }


        // Having established whether a non-null article title exists, and the presence or
        // not of a vernaculat title in a particular language, this section examines
        // the possible relationship between the two.

        if (article_title_present)
        {
            // First check if it starts with a square bracket.
            // This indicates a translation of a title originally not in English.
            // There should therefore be a vernacular title also.
            // Get the English title and any comments in brackets following the square brackets.

            if (atitle.StartsWith("["))
            {
                string poss_comment = null;

                // Strip off any final full stops from brackets, parenthesis, to make testing below easier.

                if (atitle.EndsWith("].") || atitle.EndsWith(")."))
                {
                    atitle = atitle.Substring(0, atitle.Length - 1);
                }

                if (atitle.EndsWith("]"))
                {
                    // No supplementary comment (This is almost always the case).
                    // Get the article title without brackets and expect a vernacular title.

                    atitle = atitle.Substring(1, atitle.Length - 2);  // remove the square brackets at each end
                }
                else if (atitle.EndsWith(")"))
                {
                    // Work back from the end to get the matching left parenthesis.
                    // Because the comment may itself contain parantheses necessary to
                    // match the correct left bracket.
                    // Obtain comment, and article title, and log if this seems impossible to do.

                    int bracket_count = 1;
                    for (int i = atitle.Length - 2; i >= 0; i--)
                    {
                        if (atitle[i] == '(') bracket_count--;
                        if (atitle[i] == ')') bracket_count++;
                        if (bracket_count == 0)
                        {
                            poss_comment = atitle.Substring(i + 1, atitle.Length - i - 2);
                            atitle = atitle.Substring(1, i - 2);
                            break;
                        }
                    }
                    if (bracket_count > 0)
                    {
                        string qText = "The title starts with '[', end with ')', but unable to match parentheses. Title = " 
                                       + atitle + ", for pmid {sdoid}";
                        _logger.LogLine(qText, sdoid);
                    }
                }
                else
                {
                    // Log if a square bracket at the start is not matched by an ending bracket or paranthesis.

                    string qText = "The title starts with a '[' but there is no matching ']' or ')' at the end of the title. Title = "
                                       + atitle + ", for pmid {sdoid}";
                    _logger.LogLine(qText, sdoid);
                }

                // Store the title(s) - square brackets being present.

                if (!vernacular_title_present)
                {
                    // Add the article title, without the brackets and with any comments - as the only title present it becomes the default.

                    titles.Add(new ObjectTitle(sdoid, atitle, 19, "Journal article title", "en", 11, true, poss_comment));
                }
                else
                {
                    // Both titles are present, add them both, with the vernacular title as the default.

                    titles.Add(new ObjectTitle(sdoid, atitle, 19, "Journal article title", "en", 12, false, poss_comment));
                    titles.Add(new ObjectTitle(sdoid, vtitle, 19, "Journal article title", vlang_code, 21, true, null));
                }

            }
            else
            {
                // No square brackets - should be a single article title, but sometimes not the case...
                // for example Canadian journals may have both English and French titles even if everything else is in English.

                if (vernacular_title_present)
                {
                    // Possibly something odd, vernacular title but no indication of translation in article title.
                    // Add the vernacular title, will not be the default in this case.

                    titles.Add(new ObjectTitle(sdoid, vtitle, 19, "Journal article title", vlang_code, 21, false, null));
                }

                // The most common, default situation - simply add only title as the default title record in English.

                titles.Add(new ObjectTitle(sdoid, atitle, 19, "Journal article title", "en", 11, true, null));
            }
        }
        else
        {
            // No article title at all, if there is a vernacular title use that as the default.

            if (vernacular_title_present)
            {
                titles.Add(new ObjectTitle(sdoid, vtitle, 19, "Journal article title", vlang_code, 21, true, null));
            }
        }

        // Make the art_title variable (will be used within the display title) the default title.

        if (titles.Count > 0)
        {
            foreach (ObjectTitle t in titles)
            {
                if ((bool)t.is_default)
                {
                    art_title = t.title_text;
                    break;
                }
            }
        }


        // Obtain and store publication status.
        //string publication_status = GetElementAsString(pubmed.Element("PublicationStatus"));

        // Obtain any article databank list - to identify links to
        // registries and / or gene or protein databases. Each distinct bank
        // is given an integer number (n) which is used within the 
        // DB_Accession_Number records.

        XElement databanklist = article.Element("DataBankList");
        if (databanklist != null)
        {
            int n = 0;
            foreach (XElement db in databanklist.Elements("DataBank"))
            {
                string bnkname = GetElementAsString(db.Element("DataBankName"));
                n++;

                if (db.Element("AccessionNumberList") != null)
                {
                    XElement accList = db.Element("AccessionNumberList");

                    // Get the accession numbers for this list, for this databank.
                    // Add each to the DB_Acession_Number list.

                    db_ids = accList.Elements("AccessionNumber")
                            .Select(a => new ObjectDBLink
                            {
                                sd_oid = sdoid,
                                db_sequence = n,
                                db_name = bnkname,
                                id_in_db = GetElementAsString(a)
                            }).ToList();
                }
            }
        }

        #endregion



        #region Dates

        string publication_date_string = null;    // Used to summarise the date(s) in the display title.

        // Get the publication date.
        // If non standard transfer direct to the date as a string,
        // If standard process to a standard date format.

        var pub_date = article.Element("Journal")?
                            .Element("JournalIssue")?
                            .Element("PubDate");

        if (pub_date != null)
        {
            ObjectDate publication_date = null;
            if (pub_date.Element("MedlineDate") != null)
            {
                // A string 'Medline' date, a range or a non-standard date.
                // ProcessMedlineDate is a helper function that tries to 
                // split any range.

                string date_string = pub_date.Element("MedlineDate").Value;
                publication_date = dh.ProcessMedlineDate(sdoid, date_string, 12, "Available");
            }
            else
            {
                // An 'ordinary' composite Y, M, D date.
                // ProcessDate is a helper function that splits the date components, 
                // identifies partial dates, and creates the date as a string.

                publication_date = dh.ProcessDate(sdoid, pub_date, 12, "Available");
            }
            dates.Add(publication_date);
            fob.publication_year = publication_date.start_year;
            publication_date_string = publication_date.date_as_string;
        }

        // The dates of the citation itself (not the article).

        var date_citation_created = citation.Element("DateCreated");
        if (date_citation_created != null)
        {
            dates.Add(dh.ProcessDate(sdoid, date_citation_created, 52, "Pubmed citation created"));
        }

        var date_citation_revised = citation.Element("DateRevised");
        if (date_citation_revised != null)
        {
            dates.Add(dh.ProcessDate(sdoid, date_citation_revised, 53, "Pubmed citation revised"));
        }

        var date_citation_completed = citation.Element("DateCompleted");
        if (date_citation_completed != null)
        {
            dates.Add(dh.ProcessDate(sdoid, date_citation_completed, 54, "Pubmed citation completed"));
        }


        // Article date - should be used only for electronic publication.

        string electronic_date_string = null;
        var artdates = article.Elements("ArticleDate");
        if (artdates.Count() > 0)
        {
            string date_type = null;
            IEnumerable<XElement> article_dates = article.Elements("ArticleDate");
            foreach (XElement e in article_dates)
            {
                date_type = GetAttributeAsString(e.Attribute("DateType"));

                if (date_type != null)
                {
                    if (date_type.ToLower() == "electronic")
                    {
                        // = epublish, type id 55
                        ObjectDate electronic_date = dh.ProcessDate(sdoid, e, 55, "Epublish");
                        dates.Add(electronic_date);
                        electronic_date_string = electronic_date.date_as_string;
                    }
                    else
                    {
                        string qText = "Unexpected date type (" + date_type + ") found in an article date element, pmid {sdoid}"; 
                        _logger.LogLine(qText, sdoid);
                    }
                }
            }
        }


        // Process History element with possible list of Pubmed dates.

        XElement history = pubmed.Element("History");
        if (history != null)
        {
            IEnumerable<XElement> pubmed_dates = history.Elements("PubMedPubDate");
            if (pubmed_dates.Count() > 0)
            {
                string pub_status = null;
                int date_type = 0;
                string date_type_name = null;
                foreach (XElement e in pubmed_dates)
                {
                    pub_status = GetAttributeAsString(e.Attribute("PubStatus"));
                    if (pub_status != null)
                    {
                        // get date_type
                        switch (pub_status.ToLower())
                        {
                            case "received": date_type = 17; date_type_name = "Submitted"; break;
                            case "accepted": date_type = 11; date_type_name = "Accepted"; break;
                            case "epublish":
                                {
                                    // an epublish date may already be in from article date
                                    // DateNotPresent is a helper function that indicates if a date 
                                    // of a particular type has already been provided or not.

                                    int? year = GetElementAsInt(e.Element("Year"));
                                    int? month = GetElementAsInt(e.Element("Month"));
                                    int? day = GetElementAsInt(e.Element("Day"));
                                    if (ih.DateNotPresent(dates, 55, year, month, day))
                                    {
                                        date_type = 55;
                                        date_type_name = "Epublish";
                                    }
                                    break;
                                }
                            case "ppublish": date_type = 56; date_type_name = "Ppublish"; break;
                            case "revised": date_type = 57; date_type_name = "Revised"; break;
                            case "aheadofprint": date_type = 58; date_type_name = "Ahead of print publication"; break;
                            case "retracted": date_type = 59; date_type_name = "Retracted"; break;
                            case "ecollection": date_type = 60; date_type_name = "Added to eCollection"; break;
                            case "pmc": date_type = 61; date_type_name = "Added to PMC"; break;
                            case "pubmed": date_type = 62; date_type_name = "Added to Pubmed"; break;
                            case "medline": date_type = 63; date_type_name = "Added to Medline"; break;
                            case "entrez": date_type = 65; date_type_name = "Added to entrez"; break;
                            case "pmc-release": date_type = 64; date_type_name = "PMC embargo release"; break;
                            default:
                                {
                                    date_type = 0;
                                    string qText = "An unexpexted status (" + pub_status + ") found a date in the history section, pmid {sdoid}";
                                    _logger.LogLine(qText, sdoid);
                                    break;
                                }
                        }

                        if (date_type != 0)
                        {
                            dates.Add(dh.ProcessDate(sdoid, e, date_type, date_type_name));
                        }
                    }
                }
            }
        }

        #endregion



        #region keywords

        // Chemicals list - do these first as Mesh list often duplicates them.

        XElement chemicals_list = citation.Element("ChemicalList");
        if (chemicals_list != null)
        {
            IEnumerable<XElement> chemicals = chemicals_list.Elements("Chemical");
            if (chemicals.Count() > 0)
            {
                foreach (XElement ch in chemicals)
                {
                    XElement chemName = ch.Element("NameOfSubstance");
                    string topic_ct_code = null;
                    if (chemName != null)
                    {
                        topic_ct_code = GetAttributeAsString(chemName.Attribute("UI"));
                    }

                    topics.Add(new ObjectTopic(sdoid, 12, "chemical / agent", true, 
                             topic_ct_code, GetElementAsString(chemName)));
                }
            }
        }

        // Mesh headings list.

        XElement mesh_headings_list = citation.Element("MeshHeadingList");
        if (mesh_headings_list != null)
        {
            IEnumerable<XElement> mesh_headings = mesh_headings_list.Elements("MeshHeading");
            foreach (XElement e in mesh_headings)
            {
                XElement desc = e.Element("DescriptorName");

                // Create a simple mesh heading record.

                string topic_ct_code = null, topic_orig_value = null, topic_type = null;
                if (desc != null)
                {
                    topic_orig_value = GetElementAsString(desc);
                    topic_ct_code = GetAttributeAsString(desc.Attribute("UI"));
                    topic_type = GetAttributeAsString(desc.Attribute("Type"))?.ToLower();
                }

                // Check does not already exist (if it does, usually because it was in the chemicals list)

                bool new_topic = true;
                foreach (ObjectTopic t in topics)
                {
                    if (t.original_value.ToLower() == topic_orig_value.ToLower()
                        && (t.topic_type == topic_type || (t.topic_type == "chemical / agent" && topic_type == null)))
                    {
                        new_topic = false;
                        break;
                    }
                }

                if (new_topic)
                {
                    topics.Add(new ObjectTopic(sdoid, 0, topic_type, true,
                             topic_ct_code, topic_orig_value));
                }


                /*
                 * DON'T COLLECT Qualifiers - at least for the moment
                 * 
                // if there are qualifiers, use these as the term type (or scope / context) 
                // in further copies of the keyword

                IEnumerable<XElement> qualifiers = e.Elements("QualifierName");
                if (qualifiers.Count() > 0)
                {
                    string qualcode = null, qualvalue = null;
                    foreach (XElement em in qualifiers)
                    {
                        qualcode = GetAttributeAsString(em.Attribute("UI"));
                        qualvalue = GetElementAsString(em);

                        topics.Add(new ObjectTopic(sdoid, 0, topic_type, true,
                            topic_ct_code, topic_orig_value, qualcode, qualvalue));

                    }
                }
                */
            }
        }


        // Supplementary mesh list - rarely found.

        XElement suppmesh_list = citation.Element("SupplMeshList");
        if (suppmesh_list != null)
        {
            IEnumerable<XElement> supp_mesh_names = suppmesh_list.Elements("SupplMeshName");
            if (supp_mesh_names.Count() > 0)
            {
                foreach (XElement s in supp_mesh_names)
                {
                    topics.Add(new ObjectTopic(sdoid, 0, GetAttributeAsString(s.Attribute("Type"))?.ToLower(), true,
                             GetAttributeAsString(s.Attribute("UI")), GetElementAsString(s)));

                }
            }
        }


        // Keywords

        var keywords_lists = citation.Elements("KeywordList");
        if (keywords_lists.Count() > 0)
        {
            foreach (XElement e in keywords_lists)
            {
                string this_owner = GetAttributeAsString(e.Attribute("Owner"));
                IEnumerable<XElement> words = e.Elements("Keyword");
                if (words.Count() > 0)
                {
                    foreach (XElement k in words)
                    {
                        int ct_id = (this_owner == "NOTNLM") ? 11 : 0;
                        topics.Add(new ObjectTopic(sdoid, 11, "keyword", GetElementAsString(k), ct_id, null));
                    }
                }

            }
        }
        #endregion



        #region Identifiers

        // Article Elocations - can provide doi and publishers id.

        var locations = article.Elements("ELocationID");
        string source_elocation_string = "";
        if (locations.Count() > 0)
        {
            string valid_yn = null;
            string loctype = null;
            string value = null;
            foreach (XElement t in locations)
            {
                valid_yn = GetAttributeAsString(t.Attribute("ValidYN"));
                if (valid_yn.ToUpper() == "Y")
                {
                    loctype = GetAttributeAsString(t.Attribute("EIdType"));
                    value = GetElementAsString(t);
                    if (loctype != null && value != null)
                    {
                        switch (loctype.ToLower())
                        {
                            case "pii":
                                {
                                    identifiers.Add(new ObjectIdentifier(sdoid, 34, "Publisher article ID", value, null, null));
                                    source_elocation_string += "pii:" + value + ". ";
                                    break;
                                }

                            case "doi":
                                {
                                    if (fob.doi == null) fob.doi = value.Trim();
                                    source_elocation_string += "doi:" + value + ". ";
                                    break;
                                }
                        }
                    }
                }
            }
        }


        // Other ids.

        var other_ids = citation.Elements("OtherID");
        if (other_ids.Count() > 0)
        {
            string source = null;
            string other_id = null;
            foreach (XElement i in other_ids)
            {
                source = GetAttributeAsString(i.Attribute("Source"));
                other_id = GetElementAsString(i);
                if (source != null && other_id != null)
                {
                    // Both source and value are present, 
                    // only a few source types listed as possible.

                    if (source == "NLM")
                    {
                        if (other_id.Substring(0, 3) == "PMC")
                        {
                            identifiers.Add(new ObjectIdentifier(sdoid, 31, "PMCID", other_id, 100133, "National Library of Medicine"));

                            instances.Add(new ObjectInstance(sdoid, 1, "Full resource", 100133, "National Library of Medicine",
                            "https://www.ncbi.nlm.nih.gov/pmc/articles/" + other_id.ToString(), true,
                            36, "Web text with download"));
                           
                        }
                        else
                        {
                            identifiers.Add(new ObjectIdentifier(sdoid, 32, "NIH Manuscript ID", other_id, 100134, "National Institutes of Health"));
                        }
                    }
                    else if (source == "NRCBL")
                    {
                        identifiers.Add(new ObjectIdentifier(sdoid, 33, "NRCBL", other_id, 100447, "Georgetown University"));
                    }
                }
            }
        }


        // Article id list. Can contain a variety of Ids, including (though rarely) a doi.
        // IdNotPresent is a helper function that checks that an id of a 
        // particular type has not already been extracted.

        XElement article_ids = pubmed.Element("ArticleIdList");
        if (article_ids != null)
        {
            IEnumerable<XElement> artids = article_ids.Elements("ArticleId");
            if (artids.Count() > 0)
            {
                string id_type = null;
                string other_id = null;
                foreach (XElement artid in artids)
                {
                    id_type = GetAttributeAsString(artid.Attribute("IdType"));
                    other_id = GetElementAsString(artid).Trim();
                    if (id_type != null && other_id != null)
                    {
                        switch (id_type.ToLower())
                        {
                            case "doi":
                                {
                                    if (fob.doi == null)
                                    {
                                        fob.doi = other_id;
                                    }
                                    else
                                    {
                                        if (fob.doi != other_id)
                                        {
                                            string qText = "Two different dois have been supplied: " + fob.doi +
                                                           " from ELocation, and " + other_id + " from Article Ids, pmid { sdoid}";
                                            _logger.LogLine(qText, sdoid);
                                            break;
                                        }
                                    }
                                    break;
                                }
                            case "pii":
                                {
                                    if (ih.IdNotPresent(identifiers, 34, other_id))
                                    {
                                        identifiers.Add(new ObjectIdentifier(sdoid, 34, "Publisher article ID", other_id, null, null));
                                    }
                                    break;
                                }

                            case "pmcpid": { identifiers.Add(new ObjectIdentifier(sdoid, 37, "PMC Publisher ID", other_id, null, null)); break; }

                            case "pmpid": { identifiers.Add(new ObjectIdentifier(sdoid, 38, "PM Publisher ID", other_id, null, null)); break; }

                            case "sici": { identifiers.Add(new ObjectIdentifier(sdoid, 35, "Serial Item and Contribution Identifier ", other_id, null, null)); break; }

                            case "medline": { identifiers.Add(new ObjectIdentifier(sdoid, 36, "Medline UID", other_id, 100133, "National Library of Medicine")); break; }

                            case "pubmed":
                                {
                                    if (ih.IdNotPresent(identifiers, 16, other_id))
                                    {
                                        // should be present already! - if a different value log it a a query
                                        string qText = "Two different values for pmid found: record pmiod is {sdoid}, but in article ids the value " + other_id + " is listed";
                                        _logger.LogLine(qText, sdoid);
                                        identifiers.Add(new ObjectIdentifier(sdoid, 16, "PMID", sdoid, 100133, "National Library of Medicine"));
                                    }
                                    break;
                                }
                            case "mid":
                                {
                                    if (ih.IdNotPresent(identifiers, 32, other_id))
                                    {
                                        identifiers.Add(new ObjectIdentifier(sdoid, 32, "NIH Manuscript ID", other_id, 100134, "National Institutes of Health"));
                                    }
                                    break;
                                }

                            case "pmc":
                                {
                                    if (ih.IdNotPresent(identifiers, 31, other_id))
                                    {
                                        identifiers.Add(new ObjectIdentifier(sdoid, 31, "PMCID", other_id, 100133, "National Library of Medicine"));

                                        instances.Add(new ObjectInstance(sdoid, 1, "Full resource", 100133, "National Library of Medicine",
                                        "https://www.ncbi.nlm.nih.gov/pmc/articles/" + other_id.ToString(), true,
                                        36, "Web text with download"));
                                    }
                                    break;
                                }

                            case "pmcid":
                                {
                                    if (ih.IdNotPresent(identifiers, 31, other_id))
                                    {
                                        identifiers.Add(new ObjectIdentifier(sdoid, 31, "PMCID", other_id, 100133, "National Library of Medicine"));

                                        instances.Add(new ObjectInstance(sdoid, 1, "Full resource", 100133, "National Library of Medicine",
                                        "https://www.ncbi.nlm.nih.gov/pmc/articles/" + other_id.ToString(), true,
                                        36, "Web text with download"));
                                    }
                                    break;
                                }
                            default:
                                {
                                    string qText = "A unexpexted article id type (" + id_type + ") found a date in the article id section, for pmid {sdoid}";
                                    _logger.LogLine(qText, sdoid);
                                    break;
                                }
                        }
                    }
                }
            }
        }


        // See if any article dates can be matched to the identifiers.

        foreach (ObjectIdentifier i in identifiers)
        {
            if (i.identifier_type_id == 16)
            {
                // pmid
                foreach (ObjectDate dt in dates)
                {
                    if (dt.date_type_id == 62)
                    {
                        // date added to PubMed
                        i.identifier_date = dt.date_as_string;
                        break;
                    }
                }
            }

            // pmid date may be available as an entrez date
            if (i.identifier_type_id == 16 && i.identifier_date == null)
            {
                // pmid
                foreach (ObjectDate dt in dates)
                {
                    if (dt.date_type_id == 65)
                    {
                        // date added to Entrez (normally = date added to pubMed)
                        i.identifier_date = dt.date_as_string;
                        break;
                    }
                }
            }

            if (i.identifier_type_id == 31)
            {
                // pmc id
                foreach (ObjectDate dt in dates)
                {
                    if (dt.date_type_id == 61)
                    {
                        // date added to PMC
                        i.identifier_date = dt.date_as_string;
                        break;
                    }
                }
            }

            if (i.identifier_type_id == 34)
            {
                // publisher's id
                foreach (ObjectDate dt in dates)
                {
                    if (dt.date_type_id == 11)
                    {
                        // date of acceptance by publisher into their system
                        i.identifier_date = dt.date_as_string;
                        break;
                    }

                }
            }

            if (i.identifier_type_id == 36)
            {
                // Medline UID
                foreach (ObjectDate dt in dates)
                {
                    if (dt.date_type_id == 63)
                    {
                        // date added to Medline
                        i.identifier_date = dt.date_as_string;
                        break;
                    }

                }
            }

        }

        #endregion



        #region People

        // Get author details. GetPersonalData is a helper function that
        // splits the author information up into its constituent classes.

        XElement author_list = article.Element("AuthorList");
        if (author_list != null)
        {
            var authors = author_list.Elements("Author");
            foreach (XElement a in authors)
            {
                bool valid = GetAttributeAsBool(a.Attribute("ValidYN"));
                if (valid)   // only use valid entries
                {
                    // Construct the basic contributor data from the various elements.
                    string family_name = GetElementAsString(a.Element("LastName")) ?? "";
                    string given_name = GetElementAsString(a.Element("ForeName")) ?? "";
                    string suffix = GetElementAsString(a.Element("Suffix")) ?? "";
                    string initials = GetElementAsString(a.Element("Initials")) ?? "";
                    string collective_name = GetElementAsString(a.Element("CollectiveName")) ?? "";

                    if (given_name == "")
                    {
                        given_name = initials;
                    }

                    string full_name = "";
                    if (collective_name != "")
                    {
                        family_name = collective_name;
                        full_name = collective_name;
                        given_name = "";
                    }
                    else
                    {
                        if (suffix != "") { suffix = " " + suffix; }
                        full_name = (given_name + " " + family_name + suffix).Trim();
                    }
                    full_name = sh.ReplaceApos(full_name);

                    string identifier = "", identifier_source = "";
                    if (a.Elements("Identifier").Count() > 0)
                    {
                        var person_identifiers = a.Elements("Identifier");
                        foreach (XElement e in person_identifiers)
                        {
                            identifier = GetElementAsString(e).Trim();
                            identifier_source = GetAttributeAsString(e.Attribute("Source"));

                            // should only ever be a single ORCID identifier
                            if (identifier_source == "ORCID")
                            {
                                identifier = sh.TidyORCIDId(identifier);
                                if (identifier.Length != 19)
                                {
                                    identifier = sh.TidyORCIDId2(identifier);
                                }
                                break;  // no need to look for more
                            }
                            else
                            {
                                string qText = "person " + full_name + "(linked to {sdoid}) identifier ";
                                qText += "is not an ORCID (" + identifier + " (source =" + identifier_source + "))";
                                _logger.LogLine(qText, sdoid);
                                identifier = ""; identifier_source = "";  // do not store in db
                            }
                        }
                    }

                    string affiliation = "", affil_identifier = "", affil_organisation = ""; 
                    string affil_ident_source = "";
                    if (a.Elements("AffiliationInfo").Count() > 0)
                    {
                        var person_affiliations = a.Elements("AffiliationInfo");
                        foreach (XElement e in person_affiliations)
                        {
                            affiliation = GetElementAsString(e.Element("Affiliation")) ?? "";

                            if (affiliation.Length > 400)
                            {
                                // Likely to be a compound affiliation ... do not use
                                affiliation = null;
                            }
                            else
                            {
                                affil_identifier = GetElementAsString(e.Element("Identifier")) ?? "";
                                affil_ident_source = GetAttributeAsString(e.Element("Identifier")?.Attribute("Source")) ?? "";


                                if (affil_ident_source == "INSI")
                                {
                                    /*
                                    * Needs writing to look up affil id and turn it into an organisation
                                    * ***********************************************************************
                                    */
                                }
                                else if (affil_ident_source == "GRID")
                                {
                                    /*
                                     * Needs writing to look up affil id and turn it into an organisation
                                     * ***********************************************************************
                                     */
                                }
                                else
                                {
                                    // look at affiliation string
                                    affil_organisation = sh.ExtractOrganisation(affiliation, sdoid);
                                }
                            }
                        }

                    }

                    if (identifier == "") identifier = null;
                    if (affiliation == "") affiliation = null;
                    if (affil_organisation == "") affil_organisation = null;

                    contributors.Add(new ObjectContributor(sdoid, 11, "Creator",
                                                        given_name, family_name, full_name,
                                                        identifier, affiliation, affil_organisation));

                }

            }

            // Construct author string for citation - exact form depends on numbers of ayuthors identified.

            if (contributors.Count == 1)
            {
                string initial0 = (string.IsNullOrEmpty(contributors[0].person_given_name)) ? "" : contributors[0].person_given_name.Substring(0, 1).ToUpper();
                author_string = (contributors[0].person_family_name + " " + initial0).Trim();
            }

            else if (contributors.Count == 2)
            {
                string initial0 = (string.IsNullOrEmpty(contributors[0].person_given_name)) ? "" : contributors[0].person_given_name.Substring(0, 1).ToUpper();
                string initial1 = (string.IsNullOrEmpty(contributors[1].person_given_name)) ? "" : contributors[1].person_given_name.Substring(0, 1).ToUpper();

                author_string = (contributors[0].person_family_name + " " + initial0).Trim() + " & ";
                author_string += (contributors[1].person_family_name + " " + initial1).Trim();
            }

            else if (contributors.Count == 3)
            {
                string initial0 = (string.IsNullOrEmpty(contributors[0].person_given_name)) ? "" : contributors[0].person_given_name.Substring(0, 1).ToUpper();
                string initial1 = (string.IsNullOrEmpty(contributors[1].person_given_name)) ? "" : contributors[1].person_given_name.Substring(0, 1).ToUpper();
                string initial2 = (string.IsNullOrEmpty(contributors[2].person_given_name)) ? "" : contributors[2].person_given_name.Substring(0, 1).ToUpper();

                author_string = (contributors[0].person_family_name + " " + initial0).Trim() + ", ";
                author_string += (contributors[1].person_family_name + " " + initial1).Trim() + " & ";
                author_string += (contributors[2].person_family_name + " " + initial2).Trim();
            }

            else if (contributors.Count > 3)
            {
                string initial0 = (string.IsNullOrEmpty(contributors[0].person_given_name)) ? "" : contributors[0].person_given_name.Substring(0, 1).ToUpper();
                string initial1 = (string.IsNullOrEmpty(contributors[1].person_given_name)) ? "" : contributors[1].person_given_name.Substring(0, 1).ToUpper();
                string initial2 = (string.IsNullOrEmpty(contributors[2].person_given_name)) ? "" : contributors[2].person_given_name.Substring(0, 1).ToUpper();

                author_string = (contributors[0].person_family_name + " " + initial0).Trim() + ", ";
                author_string += (contributors[1].person_family_name + " " + initial1).Trim() + ", ";
                author_string += (contributors[2].person_family_name + " " + initial2).Trim() + " et al";

            }

            author_string = author_string.Trim();

            // some contributors may be teams or groups

            if (contributors.Count > 0)
            {
                foreach (ObjectContributor oc in contributors)
                {
                    // check if a group inserted as an individual

                    string fullname = oc.person_full_name.ToLower();
                    if (ih.CheckIfOrganisation(fullname))
                    {
                        oc.organisation_name = oc.person_full_name;
                        oc.person_full_name = null;
                        oc.person_given_name = null;
                        oc.person_family_name = null;
                        oc.person_affiliation = null;
                        oc.orcid_id = null;
                        oc.is_individual = false;
                    }
                }
            }
        }
        #endregion



        #region Descriptions

        // Derive Journal source string (used as a descriptive element)...
        // Constructed as <MedlineTA>. Date;<Volume>(<Issue>):<Pagination>. <ELocationID>.
        // Needs to be extended to take into account the publication model and thus the other poossible dates
        // see https://www.nlm.nih.gov/bsd/licensee/elements_article_source.html

        if  (JournalInfo != null)
        {
            string medline_ta = GetElementAsString(JournalInfo.Element("MedlineTA"));
            medline_ta = (medline_ta == "") ? "" : medline_ta + ". ";

            string date = (publication_date_string != null) ? publication_date_string : "";

            string volume = GetElementAsString(article.Element("Journal").Element("JournalIssue").Element("Volume"));
            if (volume == null) volume = "";

            string issue = GetElementAsString(article.Element("Journal").Element("JournalIssue").Element("Issue"));
            if (issue == null)
            {
                issue = "";
            }
            else
            {
                issue = "(" + issue + ")";
            }

            string pagination = "";
            XElement pagn = article.Element("Pagination");
            if (pagn != null)
            {
                pagination = GetElementAsString(pagn.Element("MedlinePgn"));
                if (string.IsNullOrEmpty(pagination))
                {
                    pagination = "";
                }
                else
                {
                    pagination = ":" + pagination;
                    pagination = pagination.TrimEnd(';', ' ');
                }
            }

            string vip = volume + issue + pagination;
            vip = (vip == "") ? "" : vip + ". ";

            if (string.IsNullOrEmpty(source_elocation_string))
            {
                source_elocation_string = "";
            }
            else
            {
                source_elocation_string = source_elocation_string.Trim();
            }


            string public_date = "";
            string elec_date = "";
            

            switch (pub_model)
            {
                case "Print":
                    {
                        // The date is taken from the PubDate element.

                        public_date = (publication_date_string != null) ? publication_date_string + ". " : "";
                        journal_source = medline_ta + public_date + vip + source_elocation_string;
                        break;
                    }


                case "Print-Electronic":
                    {
                        // The electronic date is before the print date but the publisher has selected the print date to be the date within the citation.
                        // The date in the citation therefore comes from the print publication date, PubDate.
                        // The electronic publishing date is then shown afterwards, as "Epub YYY MMM DD".

                        public_date = (publication_date_string != null) ? publication_date_string + ". " : "";
                        elec_date = (electronic_date_string != null) ? electronic_date_string + ". " : "";
                        journal_source = medline_ta + public_date + vip + "Epub " + elec_date + source_elocation_string;
                        break;
                    }

                case "Electronic":
                    {
                        // Here there is no published hardcopy print version of the item. 
                        // If there is an ArticleDate element present in the citation it is used as the source of the publication date in the journal source string.
                        // If no ArticleDate element was provided the publication date is assumed to be that of an electronic publication.
                        // In either case there is no explicit indication that this is an electronic publication in the citation.

                        elec_date = (electronic_date_string != null) ? electronic_date_string + ". " : "";
                        if (elec_date == "")
                        {
                            elec_date = (publication_date_string != null) ? publication_date_string + ". " : "";
                        }
                        journal_source = medline_ta + elec_date + vip + source_elocation_string;
                        break;
                    }

                case "Electronic-Print":
                    {
                        // The electronic date is before the print date, but – in contrast to "Print - Electronic" – the publisher wishes the main citation date to be based on the electronic date (ArticleDate). 
                        // The source is followed by the print date notation using the content of the PubDate element.

                        public_date = (publication_date_string != null) ? publication_date_string + ". " : "";
                        elec_date = (electronic_date_string != null) ? electronic_date_string + ". " : "";
                        journal_source = medline_ta + elec_date + vip + "Print " + public_date + source_elocation_string;
                        break;
                    }

                case "Electronic-eCollection":
                    {
                        // This is an electronic publication first, followed by inclusion in an electronic collection(similar to an issue).
                        // The publisher wants articles cited by the electronic article publication date.The citation therefore uses the ArticleDate as the source of the date, 
                        // but the eCollection date can be obtained from the PubDate element.

                        public_date = (publication_date_string != null) ? publication_date_string + ". " : "";
                        elec_date = (electronic_date_string != null) ? electronic_date_string + ". " : "";
                        journal_source = medline_ta + elec_date + vip + "eCollection " + public_date + source_elocation_string;
                        break;
                    }
            }

            // add the description
            descriptions.Add(new ObjectDescription
            {
                sd_oid = sdoid,
                description_type_id = 18,
                description_type = "Journal Source String",
                description_text = journal_source.Trim(),
                lang_code = "en"
            });
        }

        #endregion



        #region Miscellaneous

        // Comment corrections list.

        XElement comments_list = citation.Element("CommentsCorrectionsList");
        if (comments_list != null)
        {
            comments = comments_list
                        .Elements("CommentsCorrections").Select(cc => new ObjectComment
                        {
                            sd_oid = sdoid,
                            ref_type = GetAttributeAsString(cc.Attribute("RefType")),
                            ref_source = GetElementAsString(cc.Element("RefSource")),
                            pmid = GetElementAsString(cc.Element("PMID")),
                            pmid_version = (cc.Element("PMID") != null) ? GetAttributeAsString(cc.Element("PMID").Attribute("Version")) : null,
                            notes = GetElementAsString(cc.Element("Note"))
                        }).ToList();
        }


        // Publication types.

        XElement publication_type_list = article.Element("PublicationTypeList");
        if (publication_type_list != null)
        {
            string type_name;
            var pub_types = publication_type_list.Elements("PublicationType");
            if (pub_types.Count() > 0)
            {
                foreach (var pub in pub_types)
                {
                    type_name = GetElementAsString(pub);
                    
                    if(!type_name.Contains("Research Support"))
                    {
                        pubtypes.Add(new ObjectPublicationType(sdoid, type_name));
                    }
                }
            }
        }

        #endregion



        // Tidy up article title and then derive the display title.

        if (art_title.EndsWith(";"))
        {
            art_title = art_title.TrimEnd(';') + ". ";
        }
        else if (!art_title.EndsWith(".") && !art_title.EndsWith("?"))
        {
            art_title = art_title + ". ";
        }
        else
        {
            art_title = art_title + " ";
        }

        fob.title = art_title.Trim();
        fob.display_title = ((author_string != "" ? author_string + ". " : "") + art_title + journal_source).Trim();

        // Tidy up doi status.

        fob.doi_status_id = (fob.doi != null) ? 1 : 5;

        // Tidy up access type.

        Boolean PMC_present = false;
        foreach (ObjectInstance i in instances)
        {
            if (i.instance_type_id == 1)
            {
                PMC_present = true;
                break;
            }
        }
        if (PMC_present)
        {
            fob.access_type_id = 11;
            fob.access_type = "Public on-screen access and download";
        }
        else
        {
            fob.access_type_id = 15;
            fob.access_type = "Restricted download";
            fob.access_details = "Not in PMC - presumed behind pay wall, but to check";
        }


        // get managing org data from eissn, pissn
        // need reference to a repo...
        // fob.managing_org_id
        // fob.managing_org


        // Assign repeating properties to citation object
        // and return the fully constructed citation object.

        fob.object_instances = instances;
        fob.object_dates = dates;
        fob.object_titles = titles;
        fob.object_identifiers = identifiers;
        fob.object_contributors = contributors;
        fob.object_descriptions = descriptions;
        fob.object_pubtypes = pubtypes;
        fob.object_topics = topics;
        fob.object_db_ids = db_ids;
        fob.object_comments = comments;

        return fob;
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



