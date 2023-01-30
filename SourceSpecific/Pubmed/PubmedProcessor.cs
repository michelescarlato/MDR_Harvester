using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MDR_Harvester.Biolincc;
using MDR_Harvester.Ctg;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Pubmed;

public class PubmedProcessor : IObjectProcessor
{
    ILoggingHelper _loggingHelper_helper;

    public PubmedProcessor(ILoggingHelper loggingHelper_helper)
    {
        _loggingHelper_helper = loggingHelper_helper;
    }
       
    public FullDataObject? ProcessData(string json_string, DateTime? download_datetime)
    {
        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        Pubmed_Record? r = JsonSerializer.Deserialize<Pubmed_Record?>(json_string, json_options);
        if (r is null)
        {
            _loggingHelper_helper.LogError($"Unable to deserialise json file to Pubmed_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }

        FullDataObject fob = new();
        string? sdoid = r.sd_oid;
        if (string.IsNullOrEmpty(sdoid))
        {
            _loggingHelper_helper.LogError($"No valid object identifier found in Pubmed_Record\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        fob.sd_oid = sdoid;
        fob.datetime_of_data_fetch = download_datetime;
        ///PubMedHelpers ph = new();

        // Establish main citation object
        // and list structures to receive data

        List<ObjectInstance> instances = new();
        List<ObjectDate> dates = new();
        List<ObjectTitle> titles = new();
        List<ObjectIdentifier> identifiers = new();
        List<ObjectTopic> topics = new();
        List<ObjectPublicationType> pubtypes = new();
        List<ObjectDescription> descriptions = new();
        List<ObjectContributor> contributors = new();
        List<ObjectComment> comments = new();
        List<ObjectDBLink> db_ids = new();

        string author_string = "";
        string journal_source = "";

        // Identify the PMID as the source data object Id (sd_oid), and also construct and add 
        // this to the 'other identifiers' list ('other' because it is not a doi).
        // The date applied may or may not be available later.

        // Add in the defaults for pubmed articles
        // ******************************************************************************************
        // Need to be careful about different typoes of articles - this may need an additional 
        // field and mechanisms to include it when collecting references... implications for the pubmed 
        // download process as well...
        // *******************************************************************************************

        fob.object_class_id = 23;
        fob.object_class = "Text";
        fob.object_type_id = 12;
        fob.object_type = "Journal Article";
        fob.add_study_contribs = false;
        fob.add_study_topics = false;
        fob.eosc_category = 0;

        identifiers.Add(new ObjectIdentifier(sdoid, 16, "PMID", sdoid, 100133, "National Library of Medicine"));

        // Set the PMID entry as an object instance 
        // (resource 40 = Web text journal abstract), add to the instances list.
       
        instances.Add(new ObjectInstance(sdoid, 100133, "National Library of Medicine",
                                  "https://www.ncbi.nlm.nih.gov/pubmed/" + sdoid, true, 
                                  40, "Web text journal abstract"));

        int? pmidVersion = r.pmid_version;
        if (pmidVersion.HasValue)
        {
            fob.version = pmidVersion.ToString();
        }
        else
        {
            _loggingHelper_helper.LogLine($"No PMID version attribute found for {sdoid}");
        }


        // Obtain and store (in the languages list) the article's language(s) - 
        // get these now as may be needed by title extraction code below.

        List<string> language_list = new();
        var languages = r.ArticleLangs;
        if (languages?.Any() is true)
        {
            string lang_list = "";
            foreach (string lang in languages)
            {
                language_list.Add(lang);
                lang_list += ", " + lang;
            }
            fob.lang_code = lang_list[2..];
        }


        // Obtain article title(s).
        // Usually just a single title present in English, but may be an additional
        // title in the 'vernacular', with a translation in English. Translated titles
        // are in square brackets and may be followed by a comment in parantheses. 
        // First set up the set of required variables.

        string? atitle = r.articleTitle;
        if (!string.IsNullOrEmpty(atitle))
        {
            atitle = atitle.ReplaceTags().ReplaceApos();
        }
        else
        {
            string qText = $"The {sdoid} citation does not have an article title";
            _loggingHelper_helper.LogLine(qText, sdoid);
        }
        string? vtitle = r.vernacularTitle;
        if (!string.IsNullOrEmpty(vtitle))
        {
            vtitle = vtitle.ReplaceTags().ReplaceApos();
        }

        // Check the vernaculat title is not the same as the article title - can happen 
        // very rarely and if it is the case the vernacular title should be ignored.

        if (atitle is not null && vtitle is not null && vtitle == atitle)
        {
            vtitle = null;
            string qText = $"The article and vernacular titles seem identical, for pmid {sdoid}";
            _loggingHelper_helper.LogLine(qText, sdoid);
        }

        // If a vernacular title try and find its language if possible - it is not given explicitly.
        // All methods imperfect but seem to work in most situations so far. First try to use a
        // listed non English language, then try the country of the journal, then see if it is
        // a Canadian journal (which may be published in ther USA).

        string vlang_code = "";
        string? journal_title = r.journalTitle;

        if (!string.IsNullOrEmpty(vtitle))
        {
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
                string? journalCountry = r.journalCountry;
                if (journalCountry is not null)
                {
                    vlang_code = journalCountry switch
                    {
                        "France" => "fr",
                        "Canada" => "fr",
                        "Germany" => "de",
                        "Spain" => "es",
                        "Mexico" => "es",
                        "Argentina" => "es",
                        "Chile" => "es",
                        "Peru" => "es",
                        "Portugal" => "pt",
                        "Brazil" => "pt",
                        "Italy" => "it",
                        "Russia" => "ru",
                        "Turkey" => "tr",
                        "Hungary" => "hu",
                        "Poland" => "pl",
                        "Sweden" => "sv",
                        "Norway" => "no",
                        "Denmark" => "da",
                        "Finland" => "fi",
                        _ => ""
                    };
                }
            }

            if (vlang_code == "" && journal_title is not null)
            {
                if (journal_title.Contains("Canada") || journal_title.Contains("Canadian"))
                {
                    vlang_code = "fr";
                }
            }
        }


        // Having established whether a non-null article title exists, and the presence or
        // not of a vernaculat title in a particular language, this section examines
        // the possible relationship between the two.

        if (atitle is not null)
        {
            // First check if it starts with a square bracket. This indicates a translation
            // of a title originally not in English. There should therefore be a vernacular title also.
            // Get the English title and any comments in brackets following the square brackets.
            // Begin by stripping any final full stops from brackets, parenthesis, to make testing below easier.

            if (atitle.StartsWith("["))
            {
                if (atitle.EndsWith("].") || atitle.EndsWith(")."))
                {
                    atitle = atitle[..^1];
                }

                string? poss_comment = null;
                if (atitle.EndsWith("]"))
                {
                    // No supplementary comment (This is almost always the case).
                    // Get the article title without brackets and expect a vernacular title.

                    atitle = atitle[1..^1];  // remove the square brackets at each end
                }
                else if (atitle.EndsWith(")"))
                {
                    // Work back from the end to get the matching left parenthesis.
                    // Because the comment may itself contain parantheses necessary to
                    // match the matching left bracket. Obtain comment, and article title,
                    // and log if this seems impossible to do.

                    int bracket_count = 1;
                    for (int i = atitle.Length - 2; i >= 0; i--)
                    {
                        if (atitle[i] == '(') bracket_count--;
                        if (atitle[i] == ')') bracket_count++;
                        if (bracket_count == 0)
                        {
                            poss_comment = atitle[(i + 1)..^1];
                            atitle = atitle[1..(i- 1)];
                            break;
                        }
                    }

                    if (bracket_count > 0)
                    {
                        string qText = $"Title '{atitle}' starts with '[', ends with ')', but unable to match parentheses, for pmid {sdoid}";
                        _loggingHelper_helper.LogLine(qText, sdoid);
                    }
                }
                else
                {
                    // Log if a square bracket at the start is not matched by an ending bracket or paranthesis.

                    string qText = "The title starts with a '[' but there is no matching ']' or ')' at the end of the title. Title = "
                                       + atitle + ", for pmid {sdoid}";
                    _loggingHelper_helper.LogLine(qText, sdoid);
                }

                // Store the title(s) - square brackets being present.

                if (string.IsNullOrEmpty(vtitle))
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

                if (!string.IsNullOrEmpty(vtitle))
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

            if (!string.IsNullOrEmpty(vtitle))
            {
                titles.Add(new ObjectTitle(sdoid, vtitle, 19, "Journal article title", vlang_code, 21, true, null));
            }
        }

        // Make the art_title variable (will be used within the display title) the default title.

        string? default_title = "";
        if (titles.Count > 0)
        {
            foreach (ObjectTitle t in titles)
            {
                if (t.is_default is true)
                {
                    default_title = t.title_text;
                    break;
                }
            }
        }


        // get some basic journal information, as this is useful for helping to 
        // determine the country of origin, and for identifying the publisher,
        // as well as later (in creating citation string). The journal name
        // and issn numbers for electronic and / or paper versions are obtained.

        JournalDetails jd = new(sdoid, journal_title ?? "");
        var issns = r.ISSNList;
        if (issns?.Any() is true)
        {
            foreach (var i in issns)
            {
                // Note the need to clean pissn / eissn numbers to a standard format.

                string? ISSN_type = i.IssnType;
                if (ISSN_type == "Print")
                {
                    string? pissn = i.Value;
                    if (pissn is not null && pissn.Length == 9 && pissn[4] == '-')
                    {
                        pissn = pissn[..4] + pissn[5..];
                    }
                    jd.pissn = pissn;
                }
                if (ISSN_type == "Electronic")
                {
                    string? eissn = i.Value;
                    if (eissn is not null && eissn.Length == 9 && eissn[4] == '-')
                    {
                        eissn = eissn[..4] + eissn[5..];
                    }
                    jd.eissn = eissn;
                }

            }
        }
        fob.journal_details = jd;


        // Obtain any article databank list - to identify links to
        // registries and / or gene or protein databases. Each distinct bank
        // is given an integer number (n) which is used within the 
        // DB_Accession_Number records.

        var databanklist =  r.DatabaseList;
        if (databanklist?.Any() is true)
        {
            int n = 0;
            foreach (var db in databanklist)
            {
                string? bankname = db.DataBankName;
                n++;
                if (db.AccessionNumberList?.Any() is true)
                {
                    foreach (string str in db.AccessionNumberList)
                    {
                        db_ids.Add(new ObjectDBLink(sdoid, n, bankname, str));
                    }
                }
            }
        }

        // Get the journal publication date.

        string publication_date_string = "";    // Used to summarise the date(s) in the display title.
        if (!string.IsNullOrEmpty(r.medlineDate))
        {
            // A string 'Medline' date, a range or a non-standard date. ProcessMedlineDate
            // is a helper function that tries to split any range.

            SplitDateRange? ml_date = PubMedHelpers.ProcessMedlineDate(r.medlineDate);
            if (ml_date is not null)
            {
                dates.Add(new ObjectDate(sdoid, 12, "Available", ml_date));
            }
            publication_date_string = r.medlineDate;
        }
        else
        {
            if (r.pubYear.HasValue)
            {
                // A composite Y, M, D date - though in this case the month is a string 

                SplitDate? pub_date = PubMedHelpers.GetSplitDateFromPubDate(r.pubYear, r.pubMonth, r.pubDay);
                if (pub_date is not null)
                {
                    dates.Add(new ObjectDate(sdoid, 12, "Available", pub_date));
                    fob.publication_year = pub_date.year;
                    publication_date_string = pub_date.date_string ?? "";
                }
            }
        }

         
        // The dates of the citation itself (not the article).

        if (r.dateCitationCompleted is not null)
        {
            NumericDate numdt = r.dateCitationCompleted;
            SplitDate? citation_date = PubMedHelpers.GetSplitDateFromNumericDate(numdt.Year, numdt.Month, numdt.Day);
            if (citation_date is not null)
            {
                dates.Add(new ObjectDate(sdoid, 54, "Pubmed citation completed", citation_date));
            }
        }

        if (r.dateCitationRevised is not null)
        {
            NumericDate numdt = r.dateCitationRevised;
            SplitDate? citation_date = PubMedHelpers.GetSplitDateFromNumericDate(numdt.Year, numdt.Month, numdt.Day);
            if (citation_date is not null)
            {
                dates.Add(new ObjectDate(sdoid, 53, "Pubmed citation revised", citation_date));
            }
        }

         
        // Article date - should be used only for electronic publication.

        string electronic_date_string = "";
        var artedates = r.ArticleEDates;
        if (artedates?.Any() is true)
        {
            foreach (var ad in artedates)
            {
                string? date_type = ad.DateType;
                if (!string.IsNullOrEmpty(date_type))
                {
                    if (date_type.ToLower() == "electronic")
                    {
                        // = epublish, type id 55
                        SplitDate? edate = PubMedHelpers.GetSplitDateFromNumericDate(ad.Year, ad.Month, ad.Day);
                        if (edate is not null)
                        {
                            dates.Add(new ObjectDate(sdoid, 55, "Epublish", edate));
                            electronic_date_string = edate.date_string ?? "";
                        }
                    }
                    else
                    {
                        string qText = $"Unexpected date type ({date_type}) found in an article date element, pmid {sdoid}"; 
                        _loggingHelper_helper.LogLine(qText, sdoid);
                    }
                }
            }
        }


       // Process History element with possible list of Pubmed dates.

        var history_dates = r.History;
        if (history_dates?.Any() is true)
        {
            string? pub_status = null;
            int date_type = 0;
            string? date_type_name = null;
            foreach (HistoryDate hd in history_dates)
            {
                pub_status = hd.PubStatus;
                if (!string.IsNullOrEmpty(pub_status))
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

                                int? year = hd.Year;
                                int? month = hd.Month;
                                int? day = hd.Day;
                                if (PubMedHelpers.DateNotPresent(dates, 55, year, month, day))
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
                                _loggingHelper_helper.LogLine(qText, sdoid);
                                break;
                            }
                    }

                    if (date_type != 0)
                    {
                        SplitDate? hd_date = PubMedHelpers.GetSplitDateFromNumericDate(hd.Year, hd.Month, hd.Day);
                        if (hd_date is not null)
                        {
                            dates.Add(new ObjectDate(sdoid, date_type, date_type_name, hd_date));
                        }
                    }
                }
            }
        }


        // Chemicals list - do these first as Mesh list often duplicates them.

        var chemicals_list = r.SubstanceList;
        if (chemicals_list?.Any() is true)
        {
            foreach (var ch in chemicals_list)
            {
                string? chemName = ch.Name;
                string? topic_ct_code = null;
                if (chemName is not null)
                {
                    topic_ct_code = ch.UI;
                }

                topics.Add(new ObjectTopic(sdoid, 12, "chemical / agent", true, topic_ct_code, chemName));
            }
        }


        // Mesh headings list. N.B. MeSH Qualifiers are not collected.

        var mesh_headings_list = r.MeshList;
        if (mesh_headings_list?.Any() is true)
        {
            foreach (var mh in mesh_headings_list)
            {
                // Create a mesh heading record.
                // Then check does not already exist (if it does,
                // usually because it was in the chemicals list)
                // before adding it to the topics list.

                string? desc = mh.Value;
                if (!string.IsNullOrEmpty(desc))
                {
                    string? topic_ct_code = mh.UI;
                    string? topic_type = mh.Type;

                    bool new_topic = true;
                    foreach (ObjectTopic t in topics)
                    {
                        if (t.original_value?.ToLower() == desc.ToLower())
                        {
                            new_topic = false;
                            break;
                        }
                    }

                    if (new_topic)
                    {
                        topics.Add(new ObjectTopic(sdoid, 0, topic_type, true, topic_ct_code, desc));
                    }
                }
            }
        }


        // Supplementary mesh list - rarely found.

        var suppmesh_list = r.SupplMeshList;
        if (suppmesh_list?.Any() is true)
        {
            foreach (var sh in suppmesh_list)
            {
                // Create a mesh heading record.
                // Then check does not already exist 
                // before adding it to the topics list.

                string? desc = sh.Value;
                if (!string.IsNullOrEmpty(desc))
                {
                    string? topic_ct_code = sh.UI;
                    string? topic_type = sh.Type;

                    bool new_topic = true;
                    foreach (ObjectTopic t in topics)
                    {
                        if (t.original_value?.ToLower() == desc.ToLower())
                        {
                            new_topic = false;
                            break;
                        }
                    }

                    if (new_topic)
                    {
                        topics.Add(new ObjectTopic(sdoid, 0, topic_type, true, topic_ct_code, desc));
                    }
                }
            }
        }


        // Keywords

        var keywords_lists = r.KeywordList;
        if (keywords_lists?.Any() is true)
        {
            string? this_owner = r.keywordOwner;
            int ct_id = (this_owner == "NOTNLM") ? 11 : 0;

            foreach (var kw in keywords_lists)
            {
                topics.Add(new ObjectTopic(sdoid, 11, "keyword", kw.Value, ct_id, null));
            }   
        }


        // Article Elocations - can provide doi and publishers id.

        var locations = r.EReferences;
        string source_elocation_string = "";
        if (locations?.Any() is true)
        {
            foreach (EReference er in locations)
            {
                string? loctype = er.EIdType;
                string? value = er.Value;
                if (loctype is not null && value is not null)
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
                                if (fob.doi is null)
                                {
                                    fob.doi = value.Trim();
                                }
                                source_elocation_string += "doi:" + value + ". ";
                                break;
                            }
                    }
                }
            }
        }


        // Other ids.

        var other_ids = r.AdditionalIds;
        if (other_ids?.Any() is true)
        {
            foreach (var i in other_ids)
            {
                string? source = i.Source;
                string? other_id = i.Value;
                if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(other_id))
                {
                    // Both source and value are present, 
                    // only a few source types listed as possible.

                    if (source == "NLM")
                    {
                        if (other_id[0..3] == "PMC")
                        {
                            identifiers.Add(new ObjectIdentifier(sdoid, 31, "PMCID", other_id, 100133, "National Library of Medicine"));

                            instances.Add(new ObjectInstance(sdoid, 100133, "National Library of Medicine",
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

        var article_ids = r.ArticleIds;
        if (article_ids?.Any() is true)
        {
            foreach (var artid in article_ids)
            {
                string? id_type = artid.IdType;
                string? other_id = artid.Value?.Trim();
                if (id_type is not null && other_id is not null)
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
                                        string qText = $"Two different dois have been supplied: {fob.doi}";
                                        qText += $" from ELocation, and {other_id} from Article Ids, pmid {sdoid}";
                                        _loggingHelper_helper.LogLine(qText, sdoid);
                                        break;
                                    }
                                }
                                break;
                            }
                        case "pii":
                            {
                                if (PubMedHelpers.IdNotPresent(identifiers, 34, other_id))
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
                                if (PubMedHelpers.IdNotPresent(identifiers, 16, other_id))
                                {
                                    // should be present already! - if a different value log it a a query
                                    string qText = "Two different values for pmid found: record pmiod is {sdoid}, but in article ids the value " + other_id + " is listed";
                                    _loggingHelper_helper.LogLine(qText, sdoid);
                                    identifiers.Add(new ObjectIdentifier(sdoid, 16, "PMID", sdoid, 100133, "National Library of Medicine"));
                                }
                                break;
                            }
                        case "mid":
                            {
                                if (PubMedHelpers.IdNotPresent(identifiers, 32, other_id))
                                {
                                    identifiers.Add(new ObjectIdentifier(sdoid, 32, "NIH Manuscript ID", other_id, 100134, "National Institutes of Health"));
                                }
                                break;
                            }

                        case "pmc":
                            {
                                if (PubMedHelpers.IdNotPresent(identifiers, 31, other_id))
                                {
                                    identifiers.Add(new ObjectIdentifier(sdoid, 31, "PMCID", other_id, 100133, "National Library of Medicine"));

                                    instances.Add(new ObjectInstance(sdoid, 100133, "National Library of Medicine",
                                    "https://www.ncbi.nlm.nih.gov/pmc/articles/" + other_id.ToString(), true,
                                    36, "Web text with download"));
                                }
                                break;
                            }

                        case "pmcid":
                            {
                                if (PubMedHelpers.IdNotPresent(identifiers, 31, other_id))
                                {
                                    identifiers.Add(new ObjectIdentifier(sdoid, 31, "PMCID", other_id, 100133, "National Library of Medicine"));

                                    instances.Add(new ObjectInstance(sdoid, 100133, "National Library of Medicine",
                                    "https://www.ncbi.nlm.nih.gov/pmc/articles/" + other_id.ToString(), true,
                                    36, "Web text with download"));
                                }
                                break;
                            }
                        default:
                            {
                                string qText = "A unexpexted article id type (" + id_type + ") found a date in the article id section, for pmid {sdoid}";
                                _loggingHelper_helper.LogLine(qText, sdoid);
                                break;
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
            else if (i.identifier_type_id == 16 && i.identifier_date == null)
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

            else if (i.identifier_type_id == 31)
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

            else if (i.identifier_type_id == 34)
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

            else if (i.identifier_type_id == 36)
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


        // Get author details. GetPersonalData is a helper function that
        // splits the author information up into its constituent classes.

        var author_list = r.Creators;
        if (author_list?.Any() is true)
        {
            foreach (var a in author_list)
            {
                // Construct the basic contributor data from the various elements.
                string? family_name = a.FamilyName.ReplaceApos(); 
                string? given_name = a.GiveneName;
                string? suffix = a.Suffix;
                string? initials = a.Initials;
                string? collective_name = a.CollectiveName;

                if (string.IsNullOrEmpty(given_name))
                {
                    given_name = initials;
                }

                string? full_name;
                if (!string.IsNullOrEmpty(collective_name))
                {
                    family_name = collective_name;
                    full_name = collective_name;
                    given_name = null;
                }
                else
                {
                    if (!string.IsNullOrEmpty(suffix)) 
                    { 
                        suffix = " " + suffix; 
                    }
                    full_name = (given_name + " " + family_name + suffix).Trim();
                }
                full_name = full_name?.ReplaceApos();

                // should only ever be a single ORCID identifier.

                string? identifier = a.IdentifierValue;
                if (!string.IsNullOrEmpty(identifier))
                {
                    string? identifier_source = a.IdentifierSource;
                    if (!string.IsNullOrEmpty(identifier_source))
                    {
                        if (identifier_source.ToLower() == "orcid")
                        {
                            identifier = identifier.TidyORCIDId();
                        }
                        else
                        {
                            string qText = $"person {full_name} (linked to {sdoid}) identifier ";
                            qText += "is not an ORCID (" + identifier + " (source =" + identifier_source + "))";
                            _loggingHelper_helper.LogLine(qText, sdoid);
                            identifier = null; identifier_source = null;  // do not store in db
                        }
                    }
                }


                string? affil_organisation = null;
                string? affiliation = null;
                var affiliations = a.AffiliationInfo;
                if (affiliations?.Any() is true)
                {
                    foreach (var aff in affiliations)
                    {
                        affiliation = aff.Affiliation;
                        if (!string.IsNullOrEmpty(affiliation))
                        {
                            if (affiliation.Length > 400)
                            {
                                // Likely to be a compound affiliation ... do not use
                                affiliation = null;
                            }
                            else
                            {
                                string? affil_identifier = aff.IdentifierValue;
                                string? affil_ident_source = aff.IdentifierSource;
                                if (affil_ident_source == "INSI")
                                {
                                    /*
                                    * Needs writing to look up affil id and turn it into an organisation
                                    * repo call through to the contextual org data
                                    * ***********************************************************************
                                    */
                                }
                                else if (affil_ident_source == "GRID")
                                {
                                    /*
                                    * Needs writing to look up affil id and turn it into an organisation
                                    * repo call through to the contextual org data
                                    * ***********************************************************************
                                    */
                                }
                                else
                                {
                                    // look at affiliation string
                                    affil_organisation = affiliation.ExtractOrganisation(sdoid);
                                }
                            }
                        }
                    }
                }

                contributors.Add(new ObjectContributor(sdoid, 11, "Creator",
                                                    given_name, family_name, full_name,
                                                    identifier, affiliation, affil_organisation));
            }

            // Construct author string for citation - exact form depends on numbers of authors identified.

            if (contributors.Count == 1)
            {
                author_string = PubMedHelpers.GetCitationName(contributors, 0);
            }

            else if (contributors.Count == 2)
            {
                author_string = PubMedHelpers.GetCitationName(contributors, 0) + " & " + PubMedHelpers.GetCitationName(contributors, 1);
            }

            else if (contributors.Count == 3)
            {
                author_string = PubMedHelpers.GetCitationName(contributors, 0) + ", " + PubMedHelpers.GetCitationName(contributors, 1)
                                + " & " + PubMedHelpers.GetCitationName(contributors, 2);
            }

            else if (contributors.Count > 3)
            {
                author_string = PubMedHelpers.GetCitationName(contributors, 0) + ", " + PubMedHelpers.GetCitationName(contributors, 1)
                                + ", " + PubMedHelpers.GetCitationName(contributors, 2) + " et al";
            }
            author_string = author_string.Trim() + ".";

            // some contributors may be teams or groups

            if (contributors.Count > 0)
            {
                foreach (ObjectContributor oc in contributors)
                {
                    // check if a group inserted as an individual

                    string? fullname = oc.person_full_name?.ToLower();
                    if (fullname.IsAnOrganisation())
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


        // Derive Journal source string (used as a descriptive element)...
        // Constructed as <MedlineTA>. Date;<Volume>(<Issue>):<Pagination>. <ELocationID>.
        // Needs to be extended to take into account the publication model and thus the other poossible dates
        // see https://www.nlm.nih.gov/bsd/licensee/elements_article_source.html

        string medline_ta = r.journalMedlineTA ?? "";
        if (medline_ta != "")
        {
            medline_ta = medline_ta + ". ";
        }

        string volume = r.journalVolume ?? "";
        string issue = r.journalIssue ?? "";
        if (issue == null)
        { 
            issue = "";
        }
        else
        {
            issue = "(" + issue + ")";
        }

        string? pagination = r.medlinePgn ?? "";   
        if (!string.IsNullOrEmpty(pagination))
        {
            pagination = ":" + pagination;
            pagination = pagination.TrimEnd(';', ' ');
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
            
        switch (r.pubModel)
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
    


        // Comment corrections list.

        var comments_list = r.CorrectionsList;
        if (comments_list?.Any() is true)
        {
            foreach (var comm in comments_list)
            {
                string? ref_type = comm.RefType;
                string? ref_source = comm.RefSource;
                string? pmid = comm.PMID_Value is null ? "" : comm.PMID_Value.ToString();
                string? pmid_version = comm.PMID_Version is null ? "" : comm.PMID_Version.ToString();
                string? notes = comm.Note; // *********************************************************
                comments.Add(new ObjectComment(sdoid, ref_type, ref_source, pmid, pmid_version, notes));
            }
        }


        // Publication types.

        var publication_type_list = r.ArticleTypes;
        if (publication_type_list?.Any() is true)
        {
            foreach (var pub in publication_type_list)
            {
                string? type_name = pub.Value;
                if (!string.IsNullOrEmpty(type_name) && !type_name.Contains("Research Support"))
                {
                    pubtypes.Add(new ObjectPublicationType(sdoid, type_name));
                }
            }
        }


        // Tidy up article title and then derive the display title.

        if (!string.IsNullOrEmpty(default_title))
        {
            if (default_title.EndsWith(";"))
            {
                default_title = default_title.TrimEnd(';') + ". ";
            }
            else if (!default_title.EndsWith(".") && !default_title.EndsWith("?"))
            {
                default_title = default_title + ". ";
            }
            else
            {
                default_title = default_title + " ";
            }
        }

        fob.title = default_title?.Trim();
        fob.display_title = ((author_string != "" ? author_string + ". " : "") + default_title + journal_source).Trim();

        // Tidy up doi status.

        fob.doi_status_id = (fob.doi != null) ? 1 : 5;

        // Tidy up access type.

        Boolean PMC_present = false;
        foreach (ObjectInstance i in instances)
        {
            if (i.resource_type_id == 36)
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
}



