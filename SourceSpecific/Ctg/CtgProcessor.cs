using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Ctg;

public class CTGProcessor : IStudyProcessor
{
    public Study? ProcessData(string json_string, DateTime? download_datetime, ILoggingHelper _logging_helper)
    {
        ///////////////////////////////////////////////////////////////////////////////////////
        // Deserialise string representing sdtudy details
        ///////////////////////////////////////////////////////////////////////////////////////

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        CTG_Record? r = JsonSerializer.Deserialize<CTG_Record?>(json_string, json_options);
        if (r is null)
        {
            _logging_helper.LogError(
                $"Unable to deserialise json file to Ctg_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Set up
        ///////////////////////////////////////////////////////////////////////////////////////

        Study s = new Study();

        List<StudyIdentifier> identifiers = new();
        List<StudyTitle> titles = new();
        List<StudyOrganisation> organisations = new();
        List<StudyPerson> people = new();
        List<StudyReference> references = new();
        List<StudyLink> studylinks = new();
        List<AvailableIPD> ipd_info = new();
        List<StudyTopic> topics = new();
        List<StudyFeature> features = new();
        List<StudyRelationship> relationships = new();
        List<StudyLocation> sites = new();
        List<StudyCountry> countries = new();
        List<StudyCondition> conditions = new();
        List<StudyIEC> iec = new();

        List<DataObject> data_objects = new();
        List<ObjectDataset> object_datasets = new();
        List<ObjectTitle> object_titles = new();
        List<ObjectDate> object_dates = new();
        List<ObjectInstance> object_instances = new();

        CTGHelpers ctgh = new();

        ProtocolSection? ps = r.protocolSection;
        SponsorCollaboratorsModule? SponsorCollaboratorsModule = ps?.sponsorCollaboratorsModule;
        DescriptionModule? DescriptionModule = ps?.descriptionModule;
        ConditionsModule? ConditionsModule = ps?.conditionsModule;
        DesignModule? DesignModule = ps?.designModule;
        EligibilityModule? EligibilityModule = ps?.eligibilityModule;
        ContactsLocationsModule? ContactsLocationsModule = ps?.contactsLocationsModule;
        ReferencesModule? ReferencesModule = ps?.referencesModule;
        IPDSharingStatementModule? IPDSharingModule = ps?.ipdSharingStatementModule;

        DocumentSection? d = r.documentSection;
        LargeDocumentModule? LargeDocumentModule = d?.largeDocumentModule;

        DerivedSection? v = r.derivedSection;
        ConditionBrowseModule? ConditionBrowseModule = v?.conditionBrowseModule;
        InterventionBrowseModule? InterventionBrowseModule = v?.interventionBrowseModule;

        IdentificationModule? IdentificationModule = ps?.identificationModule;
        StatusModule? StatusModule = ps?.statusModule;


        ///////////////////////////////////////////////////////////////////////////////////////
        // Basics - id, Submission date, NCT identifier
        ///////////////////////////////////////////////////////////////////////////////////////

        if (IdentificationModule is null || StatusModule is null)
        {
            _logging_helper.LogError(
                $"No valid Identification or Status module found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        string sid = IdentificationModule.nctId!;

        if (string.IsNullOrEmpty(sid))
        {
            _logging_helper.LogError(
                $"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        // This date is a simple field in the status module
        // assumed to be the date the identifier was assigned.

        string? submissionDate = StatusModule.studyFirstSubmitDate;

        // add the NCT identifier record - 100120 is the id of ClinicalTrials.gov.

        submissionDate = submissionDate.StandardiseCTGDateString();
        identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100120,
            "ClinicalTrials.gov", submissionDate, null));


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study Title(s)
        ///////////////////////////////////////////////////////////////////////////////////////

        string? brief_title = IdentificationModule.briefTitle?.LineClean();
        string? official_title = IdentificationModule.officialTitle?.LineClean();
        string? acronym = IdentificationModule.acronym?.Trim();

        if (!string.IsNullOrEmpty(brief_title))
        {
            titles.Add(new StudyTitle(sid, brief_title, 15, "Registry public title", true, "From Clinicaltrials.gov"));
            s.display_title = brief_title;

            if (!string.IsNullOrEmpty(official_title)
                && !string.Equals(official_title, brief_title, StringComparison.CurrentCultureIgnoreCase))
            {
                titles.Add(new StudyTitle(sid, official_title, 16, "Registry scientific title", false,
                    "From Clinicaltrials.gov"));
            }

            if (!string.IsNullOrEmpty(acronym) && !string.IsNullOrEmpty(official_title)
                                               && !string.Equals(acronym, brief_title,
                                                   StringComparison.CurrentCultureIgnoreCase)
                                               && !string.Equals(acronym, official_title,
                                                   StringComparison.CurrentCultureIgnoreCase))
            {
                titles.Add(
                    new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From Clinicaltrials.gov"));
            }
        }
        else
        {
            // No Brief Title.

            if (!string.IsNullOrEmpty(official_title))
            {
                titles.Add(new StudyTitle(sid, official_title, 16, "Registry scientific title", true,
                    "From Clinicaltrials.gov"));
                s.display_title = official_title;

                if (!string.IsNullOrEmpty(acronym) && acronym.ToLower() != official_title.ToLower())
                {
                    titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false,
                        "From Clinicaltrials.gov"));
                }
            }
            else
            {
                // Only an acronym present (very rare).

                titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", true,
                    "From Clinicaltrials.gov"));
                s.display_title = acronym;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Sponsor, Study leads, Collaborators
        ///////////////////////////////////////////////////////////////////////////////////////

        // Before getting sponsor and secondary Ids it is useful to clarify the sponsor from the 
        // sponsor collaborator's module. The sponsor as listed in the ID section should then be the same,
        // but is sometimes missing from the ID section.

        string? rp_name = ""; // responsible party's name - define here to allow later comparison
        string? sponsor_name = null; // defined here to allow later comparison

        if (SponsorCollaboratorsModule != null)
        {
            // Obtain the sponsor name and add to database.
            // In general org Id will be added later in coding process

            string? sponsor_candidate = SponsorCollaboratorsModule.leadSponsor?.name;
            if (sponsor_candidate.IsNotPlaceHolder() && sponsor_candidate.AppearsGenuineOrgName())
            {
                sponsor_name = sponsor_candidate.TidyOrgName(sid);
                if (sponsor_name == "[Redacted]")
                {
                    sponsor_name = "Sponsor name redacted in registry record";
                    organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", 13, sponsor_name));
                }
                else
                {
                    organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", null, sponsor_name));
                }
            }

            var resp_party = SponsorCollaboratorsModule.responsibleParty;
            if (resp_party is not null)
            {
                string? rp_type = resp_party.type;
                if (rp_type != "Sponsor")
                {
                    rp_name = resp_party.investigatorFullName;
                    string? rp_affil = resp_party.investigatorAffiliation;
                    string? rp_old_name_title = resp_party.oldNameTitle;
                    string? rp_old_org = resp_party.oldOrganization;

                    if (string.IsNullOrEmpty(rp_name) && !string.IsNullOrEmpty(rp_old_name_title))
                    {
                        rp_name = rp_old_name_title; // use old versions / format if necessary
                    }

                    if (string.IsNullOrEmpty(rp_affil) && !string.IsNullOrEmpty(rp_old_org))
                    {
                        rp_affil = rp_old_org; // use old versions / format if necessary
                    }

                    if (!string.IsNullOrEmpty(rp_name) && rp_name != "[Redacted]")
                    {
                        if (rp_name.AppearsGenuinePersonName())
                        {
                            rp_name = rp_name.TidyPersonName();
                            if (!string.IsNullOrEmpty(rp_name))
                            {
                                string? affil_organisation = null;
                                if (!string.IsNullOrEmpty(rp_affil)
                                    && rp_affil.IsNotPlaceHolder() && rp_affil.AppearsGenuineOrgName())
                                {
                                    // Initially. compare affiliation with sponsor.
                                    // If they do not appear to be the same try to extract the 
                                    // organisation from the affiliation string.

                                    rp_affil = rp_affil.TidyOrgName(sid);
                                    if (!string.IsNullOrEmpty(sponsor_name)
                                        && rp_affil!.ToLower().Contains(sponsor_name.ToLower()))
                                    {
                                        affil_organisation = sponsor_name;
                                    }
                                    else
                                    {
                                        affil_organisation = rp_affil!.ExtractOrganisation(sid);
                                    }
                                }

                                if (rp_type == "Principal Investigator")
                                {
                                    people.Add(new StudyPerson(sid, 51, "Study Lead",
                                        rp_name, rp_affil, null, affil_organisation));
                                }
                                else if (rp_type == "Sponsor-Investigator")
                                {
                                    people.Add(new StudyPerson(sid, 70, "Sponsor-investigator",
                                        rp_name, rp_affil, null, affil_organisation));
                                }
                            }
                        }
                    }
                }
            }

            var collaborators = SponsorCollaboratorsModule.collaborators;
            if (collaborators?.Any() is true)
            {
                foreach (var col in collaborators)
                {
                    string? collab_candidate = col.name;
                    if (collab_candidate.IsNotPlaceHolder() && collab_candidate.AppearsGenuineOrgName())
                    {
                        string? collab_name = collab_candidate?.TidyOrgName(sid);
                        organisations.Add(new StudyOrganisation(sid, 69,
                            "Collaborating organisation", null, collab_name));
                    }
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Identifiers
        ///////////////////////////////////////////////////////////////////////////////////////

        // Get the sponsor id information. The sponsor name should be in the organization field,
        // which also has the organisation type
        // while the OrgStudyId Info has details on the identifier itself (value = org_study_id)
        // and its type, and any link.

        string? pri_id_org = IdentificationModule.organization?.fullName?.TidyOrgName(sid);
        string? pri_id_value = IdentificationModule.orgStudyIdInfo?.id?.Trim();
        string? pri_id_type = IdentificationModule.orgStudyIdInfo?.type;
        string? pri_id_link = IdentificationModule.orgStudyIdInfo?.link;
        
        // Add the sponsor's identifier for the study.

        if (!string.IsNullOrEmpty(pri_id_value))
        {
            // Establish a flag (add_id) to indicate whether or not to add the id value provided.
            // Occasionally 'nil values' inserted and therefore need to be trapped.

            bool add_id = true;
            string pri_id_value_lc = pri_id_value.ToLower();
            if (pri_id_value_lc is "pending" or "nd" or "na" or "n/a" or "n.a."
                or "none" or "n/a." or "no" or "none" or "pending")
            {
                add_id = false;
            }

            if (pri_id_value_lc.StartsWith("not ") || pri_id_value_lc.StartsWith("to be ")
                                                   || pri_id_value_lc.StartsWith("not-") || pri_id_value_lc.StartsWith("not_")
                                                   || pri_id_value_lc.StartsWith("notapplic") || pri_id_value_lc.StartsWith("notavail")
                                                   || pri_id_value_lc.StartsWith("tobealloc") || pri_id_value_lc.StartsWith("tobeapp"))
            {
                add_id = false;
            }

            // Rarely, people put the same name in both org and org_study_id fields...
            // i.e. they put the organisation name in as the identifier value, or vice versa -
            // as not easy to know which, better not add the potentially false identifier.

            if (pri_id_org is not null
                && String.Equals(pri_id_value, pri_id_org, StringComparison.CurrentCultureIgnoreCase))
            {
                add_id = false;
            }

            if (add_id)
            {
                // use identifier type as a default indicator of type and organisation. This may be changed later
                // by a detailed examination of the identifier (as many seem to be wrongly classified). At
                // present only NIH, AHRQ and FDA appear to be used. The default is to assume, in the absence 
                // of a specified type, that the identifier is a sponsor identifier. The great majority of these
                // Ids are therefore initially classified as sponsor Ids.

                var (ident_type_id, ident_type, ident_org_id, ident_org) = pri_id_type switch
                {
                    "NIH" => new Tuple<int, string, int, string>(13, "Funder / Contract ID",
                        100134, "National Institutes of Health"),
                    "FDA" => new Tuple<int, string, int, string>(13, "Funder / Contract ID",
                        108548, "United States Food and Drug Administration"),
                    "VA" => new Tuple<int, string, int, string>(13, "Funder / Contract ID",
                        100224, "US Department of Veterans Affairs"),
                    "CDC" => new Tuple<int, string, int, string>(13, "Funder / Contract ID",
                        100245, "Centers for Disease Control and Prevention"),
                    "AHRQ" => new Tuple<int, string, int, string>(13, "Funder / Contract ID",
                        100407, "Agency for Healthcare Research and Quality"),
                    "SAMHSA" => new Tuple<int, string, int, string>(13, "Funder / Contract ID",
                        108270, "Substance Abuse and Mental Health Services Administration"),
                    _ => new Tuple<int, string, int, string>(14, "Sponsor’s ID", 0, "")
                };

                if (ident_type_id != 14)
                {
                    identifiers.Add(new StudyIdentifier(sid, pri_id_value, ident_type_id, ident_type,
                        ident_org_id, ident_org, null, pri_id_link));
                }
                else
                {
                    // Is a 'sponsor Id' but some special cases need to be considered 
                    // In general a sponsor name is provided but at this stage not an Id
                    // (will mostly be added in the coding process)

                    if (string.IsNullOrEmpty(pri_id_org))
                    {
                        if (!string.IsNullOrEmpty(sponsor_name))
                        {
                            identifiers.Add(new StudyIdentifier(sid, pri_id_value, 14, "Sponsor’s ID",
                                null, sponsor_name, null, pri_id_link));
                        }
                        else
                        {
                            string dummy_org_name = "No organisation name provided in source data";
                            identifiers.Add(new StudyIdentifier(sid, pri_id_value, 14, "Sponsor’s ID",
                                12, dummy_org_name, null, pri_id_link));
                        }
                    }
                    else if (pri_id_org == "[Redacted]")
                    {
                        string dummy_org_name = "Sponsor name redacted in registry record";
                        identifiers.Add(new StudyIdentifier(sid, pri_id_value, 14, "Sponsor’s ID",
                            13, dummy_org_name, null, pri_id_link));
                    }
                    else
                    {
                        identifiers.Add(new StudyIdentifier(sid, pri_id_value, 14, "Sponsor’s ID",
                            null, pri_id_org, null, pri_id_link));
                    }
                }
            }
        }

        // add any additional identifiers (if not already used as a sponsor id).

        var secIds = IdentificationModule.secondaryIdInfos;
        if (secIds?.Any() is true)
        {
            foreach (var sec_id in secIds)
            {
                string? sec_id_org = sec_id.domain?.TidyOrgName(sid);
                string? sec_id_value = sec_id.id?.Trim();
                string? sec_id_type = sec_id.type;
                string? sec_id_link = sec_id.link;

                if (!string.IsNullOrEmpty(sec_id_value))
                {
                    // Check not already used as the sponsor id (= pri_id_value)
                    // or is a copy of the NCT Id (it happens!)

                    if (!string.IsNullOrEmpty(pri_id_value) &&
                        string.Equals(pri_id_value, sec_id_value, StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }
                    if (sec_id_value == sid)
                    {
                        continue;
                    }
                    
                    // In a few cases a secondary id appears to be a different NCT number
                    // If true, implies a relationship between the two studies rather than
                    // a 'secondary id'
                    
                    //********************************************************************
                    //********************************************************************

                    // use the 'type' of identifier to provide a baseline categorisation of
                    // the id - though some may be reclassified later after inspection of the 
                    // id or organisation in detail. Note that the bulk of Ids fit into the
                    // categories 'Other' or (most commonly) have no type defined.

                    var (ident_type_id, ident_type, ident_org_id, ident_org) = sec_id_type switch
                    {
                        "NIH" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            100134, "National Institutes of Health"),
                        "FDA" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            108548, "United States Food and Drug Administration"),
                        "VA" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            100224, "US Department of Veterans Affairs"),
                        "CDC" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            100245, "Centers for Disease Control and Prevention"),
                        "AHRQ" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            100407, "Agency for Healthcare Research and Quality"),
                        "SAMHSA" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            108270, "Substance Abuse and Mental Health Services Administration"),
                        "OTHER_GRANT" => new Tuple<int, string, int?, string?>(13, "Funder / Contract ID",
                            null, sec_id_org),
                        "EUDRACT_NUMBER" => new Tuple<int, string, int?, string?>(11, "Trial Registry ID",
                            100123, "EU Clinical Trials Register"),
                        "REGISTRY" => new Tuple<int, string, int?, string?>(11, "Trial Registry ID",
                            null, sec_id_org),
                        "OTHER" => new Tuple<int, string, int?, string?>(90, "Other", null, sec_id_org),
                        _ => new Tuple<int, string, int?, string?>(1, "No type given in source data", null, sec_id_org)
                    };

                    string? org_lower = sec_id_org?.ToLower();
                    if (sec_id_org is null || org_lower is "other" or "alias study number")
                    {
                        identifiers.Add(new StudyIdentifier(sid, sec_id_value,
                            12, "No organisation name provided in source data",
                            null, null, null, sec_id_link));
                    }
                    else if (org_lower is "company internal" or "sponsor")
                    {
                        // need to use sponsor name or primary id sponsor name from above

                        if (!string.IsNullOrEmpty(sponsor_name))
                        {
                            identifiers.Add(new StudyIdentifier(sid, sec_id_value, 14, "Sponsor’s ID",
                                null, sponsor_name, null, sec_id_link));
                        }
                        else if (!string.IsNullOrEmpty(pri_id_org))
                        {
                            identifiers.Add(new StudyIdentifier(sid, sec_id_value, 14, "Sponsor’s ID",
                                null, pri_id_org, null, sec_id_link));
                        }
                        else
                        {
                            string dummy_org_name = "No organisation name provided in source data";
                            identifiers.Add(new StudyIdentifier(sid, sec_id_value, 14, "Sponsor’s ID",
                                12, dummy_org_name, null, null));
                        }
                    }
                    else
                    {
                        identifiers.Add(new StudyIdentifier(sid, sec_id_value, ident_type_id, ident_type,
                            ident_org_id, ident_org, null, sec_id_link));
                    }
                }
            }

            // and then go through ALL identifiers (including the claimed sponsor Id but excepting
            // the initial NCT one) to see if they can be better characterised

            foreach (StudyIdentifier si in identifiers)
            {
                if (si.identifier_value != sid)
                {
                    IdentifierDetails idd = ctgh.ProcessCTGIdentifier(si);
                    if (idd.changed)
                    {
                        si.identifier_value = idd.id_value;
                        si.identifier_type_id = idd.id_type_id;
                        si.identifier_type = idd.id_type;
                        si.source_id = idd.id_org_id;
                        si.source = idd.id_org;
                    }
                }
            }

            // Also add any NCT aliases (obsolete Ids).

            var obsoleteIds = IdentificationModule.nctIdAliases;
            if (obsoleteIds?.Any() is true)
            {
                foreach (string obsId in obsoleteIds)
                {
                    identifiers.Add(new StudyIdentifier(sid, obsId, 44, "Obsolete NCT number",
                        100120, "ClinicalTrials.gov", null, null));
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study dates
        ///////////////////////////////////////////////////////////////////////////////////////

        SplitDate? first_post = null;
        SplitDate? results_post = null;
        SplitDate? update_post = null;

        var FirstPostDate = StatusModule.studyFirstPostDateStruct;
        string? first_post_type = FirstPostDate?.type;
        if (first_post_type is "Actual" or "Estimate")
        {
            first_post = FirstPostDate?.date?.GetDatePartsFromISOString();
            if (first_post is not null && first_post_type == "Estimate")
            {
                first_post.date_string += " (est.)";
            }
        }

        bool results_present = false;

        var ResultsPostDate = StatusModule.resultsFirstPostDateStruct;
        string? results_type = ResultsPostDate?.type;
        if (results_type is "Actual" or "Estimate")
        {
            results_post = ResultsPostDate?.date?.GetDatePartsFromISOString();
            if (results_post is not null && results_type == "Estimate")
            {
                results_post.date_string += " (est.)";
            }

            // Assumption is that if results are available the results post
            // must be present with an actual or estimated (not anticipated) date.

            results_present = true;
        }

        var LastUpdateDate = StatusModule.lastUpdatePostDateStruct;
        if (LastUpdateDate is not null)
        {
            string? update_type = LastUpdateDate.type;
            if (update_type is "Actual" or "Estimate")
            {
                update_post = LastUpdateDate.date?.GetDatePartsFromISOString();
                if (update_post is not null && update_type == "Estimate")
                {
                    update_post.date_string += " (est.)";
                }
            }
        }

        // Store study start date, if available

        var StudyStartDate = StatusModule.startDateStruct;
        if (StudyStartDate is not null)
        {
            SplitDate? start_date = StudyStartDate.date?.GetDatePartsFromISOString();
            s.study_start_year = start_date?.year;
            s.study_start_month = start_date?.month;
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Expanded access details
        ///////////////////////////////////////////////////////////////////////////////////////

        var ExpandedAccessInfo = StatusModule.expandedAccessInfo;
        string? expanded_access_nct_id = ExpandedAccessInfo?.nctId?.Trim();
        if (expanded_access_nct_id != null)
        {
            relationships.Add(new StudyRelationship(sid, 23, "has an expanded access version",
                expanded_access_nct_id));
            relationships.Add(new StudyRelationship(expanded_access_nct_id, 24, "is an expanded access version of",
                sid));
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Study status
        ///////////////////////////////////////////////////////////////////////////////////////

        s.study_status = StatusModule.overallStatus;
        s.study_status_id = s.study_status.GetStatusId();
        string? status_verified_date = StatusModule.statusVerifiedDate;


        ///////////////////////////////////////////////////////////////////////////////////////
        // Brief description
        ///////////////////////////////////////////////////////////////////////////////////////

        if (DescriptionModule is not null)
        {
            s.brief_description = DescriptionModule.briefSummary.FullClean(); // need to remove \\ sequences
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Topics and Conditions
        ///////////////////////////////////////////////////////////////////////////////////////

        var browser_conds = ConditionBrowseModule?.meshes;
        if (browser_conds?.Any() is true)
        {
            foreach (var con in browser_conds)
            {
                string? mesh_code = con.id;
                string? mesh_term = con.term;
                conditions.Add(new StudyCondition(sid, mesh_term, 14, "MeSH", mesh_code));
            }
        }

        var conds = ConditionsModule?.conditions;
        if (conds?.Any() is true)
        {
            foreach (string condition in conds)
            {
                if (condition_is_new(condition)) // only add the condition name if not already present.
                {
                    conditions.Add(new StudyCondition(sid, condition));
                }
            }
        }

        var interventions = InterventionBrowseModule?.meshes;
        if (interventions?.Any() is true)
        {
            foreach (var interv in interventions)
            {
                string? mesh_code = interv.id;
                string? mesh_term = interv.term;
                topics.Add(new StudyTopic(sid, 12, "chemical / agent", mesh_code, mesh_term));
            }
        }


        var keywords = ConditionsModule?.keywords;
        if (keywords?.Any() is true)
        {
            foreach (string keyword in keywords)
            {
                // Regularise drug name
                string k_word = keyword; // need to do this indirectly as cannot alter the foreach variable
                if (k_word.Contains(((char)174).ToString()))
                {
                    // add to CleanLine?

                    k_word = k_word.Replace(((char)174).ToString(), ""); // drop reg mark
                    k_word = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(k_word.ToLower());
                }

                // only add the keyword if not already present in the topics or conditions

                if (topic_is_new(k_word) && condition_is_new(k_word))
                {
                    topics.Add(new StudyTopic(sid, 11, "keyword", k_word));
                }
            }
        }

        bool topic_is_new(string candidate_topic)
        {
            foreach (StudyTopic k in topics)
            {
                if (String.Equals(k.original_value!, candidate_topic,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        bool condition_is_new(string candidate_condition)
        {
            foreach (StudyCondition k in conditions)
            {
                if (String.Equals(k.original_value!, candidate_condition,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Study design
        ///////////////////////////////////////////////////////////////////////////////////////

        if (DesignModule is not null)
        {
            s.study_type = DesignModule.studyType;
            s.study_type_id = s.study_type.GetTypeId();

            if (s.study_type == "Interventional")
            {
                var Phaselist = DesignModule.phases;
                if (Phaselist?.Any() is true)
                {
                    foreach (string phase in Phaselist)
                    {
                        features.Add(new StudyFeature(sid, 20, "phase", phase.GetPhaseId(), phase));
                    }
                }
                else
                {
                    string phase = "Not provided";
                    features.Add(new StudyFeature(sid, 20, "phase", phase.GetPhaseId(), phase));
                }


                var design_info = DesignModule.designInfo;
                if (design_info is not null)
                {
                    string design_allocation = design_info.allocation ?? "Not provided";
                    features.Add(new StudyFeature(sid, 22, "allocation type",
                        design_allocation.GetAllocationTypeId(), design_allocation));

                    string design_intervention_model = design_info.interventionModel ?? "Not provided";
                    features.Add(new StudyFeature(sid, 23, "intervention model",
                        design_intervention_model.GetDesignTypeId(), design_intervention_model));

                    string design_primary_purpose = design_info.primaryPurpose ?? "Not provided";
                    features.Add(new StudyFeature(sid, 21, "primary purpose",
                        design_primary_purpose.GetPrimaryPurposeId(), design_primary_purpose));

                    var masking_details = design_info.maskingInfo;
                    string design_masking = "Not provided"; // default value             
                    if (masking_details != null)
                    {
                        design_masking = masking_details.masking ?? "Not provided";
                    }

                    features.Add(new StudyFeature(sid, 24, "masking", design_masking.GetMaskingTypeId(),
                        design_masking));
                }
            }


            if (s.study_type == "Observational")
            {
                bool? patient_registry = DesignModule.patientRegistry;
                if (patient_registry == true) // change type...
                {
                    s.study_type_id = 13;
                    s.study_type = "Observational Patient Registry";
                }

                var design_info = DesignModule.designInfo;
                if (design_info is not null)
                {
                    var obs_model = design_info.observationalModel;
                    if (obs_model is not null)
                    {
                        features.Add(new StudyFeature(sid, 30, "observational model",
                            obs_model.GetObsModelTypeId(), obs_model));
                    }
                    else
                    {
                        obs_model = "Not provided";
                        features.Add(new StudyFeature(sid, 30, "observational model",
                            obs_model.GetObsModelTypeId(), obs_model));
                    }

                    var time_persp = design_info.timePerspective;
                    if (time_persp is not null)
                    {
                        features.Add(new StudyFeature(sid, 31, "time perspective",
                            time_persp.GetTimePerspectiveId(), time_persp));
                    }
                    else
                    {
                        time_persp = "Not provided";
                        features.Add(new StudyFeature(sid, 31, "time perspective",
                            time_persp.GetTimePerspectiveId(), time_persp));
                    }
                }

                var biospec_details = DesignModule.bioSpec;
                if (biospec_details is not null)
                {
                    // add - description?
                    string biospecs = biospec_details.retention ?? "Not provided";
                    features.Add(new StudyFeature(sid, 32, "biospecimens retained",
                        biospecs.GetSpecimenRetentionId(), biospecs));
                }
            }

            var enrol_details = DesignModule.enrollmentInfo;
            if (enrol_details is not null)
            {
                string? enrolment_count = enrol_details.count.ToString();
                if (!string.IsNullOrEmpty(enrolment_count))
                {
                    // also check it is not just a string of 9s

                    if (!Regex.Match(enrolment_count, @"^9+$").Success)
                    {
                        s.study_enrolment = enrolment_count;
                    }

                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Eligibility
        ///////////////////////////////////////////////////////////////////////////////////////

        if (EligibilityModule is not null)
        {
            s.study_gender_elig = EligibilityModule.sex ?? "Not provided";
            if (s.study_gender_elig == "All")
            {
                s.study_gender_elig = "Both";
            }

            s.study_gender_elig_id = s.study_gender_elig.GetGenderEligId();

            string? min_age = EligibilityModule.minimumAge;
            if (min_age is not null)
            {
                // split number from time unit
                string min_ageT = min_age.Trim();
                int space_pos = min_ageT.IndexOf(' ');
                string LHS = min_ageT[..space_pos];
                string RHS = min_ageT[(space_pos + 1)..];
                if (Int32.TryParse(LHS, out int minage))
                {
                    s.min_age = minage;
                    if (!RHS.EndsWith("s")) RHS += "s";
                    s.min_age_units = RHS;
                    s.min_age_units_id = RHS.GetTimeUnitsId();
                }
            }

            string? max_age = EligibilityModule.maximumAge;
            if (max_age is not null)
            {
                string max_ageT = max_age.Trim();
                int space_pos = max_ageT.IndexOf(' ');
                string LHS = max_ageT[..space_pos];
                string RHS = max_ageT[(space_pos + 1)..];
                if (Int32.TryParse(LHS, out int maxage))
                {
                    s.max_age = maxage;
                    if (!RHS.EndsWith("s")) RHS += "s";
                    s.max_age_units = RHS;
                    s.max_age_units_id = RHS.GetTimeUnitsId();
                }
            }


            ///////////////////////////////////////////////////////////////////////////////////////
            // Inclusion / Exclusion criteria
            ///////////////////////////////////////////////////////////////////////////////////////

            int study_iec_type = 0;
            string? elig_statement = EligibilityModule.eligibilityCriteria;
            if (elig_statement is not null)
            {
                elig_statement.FullClean();
                string elig_low = elig_statement.ToLower();
                if (elig_low.Contains("inclusion") && elig_low.Contains("exclusion"))
                {
                    // Need to be not too close too each other and not too near the end
                    int inc_pos = elig_low.IndexOf("inclusion", 0, StringComparison.Ordinal);
                    int exc_pos = elig_low.IndexOf("exclusion", 0, StringComparison.Ordinal);
                    if (exc_pos - inc_pos > 12
                        && elig_statement.Length - exc_pos > 4 && elig_statement.Length - exc_pos > 20)
                    {
                        // try and split on "exclusion"
                        int num_inc_criteria = 0;
                        string ic = elig_statement[..exc_pos];
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
                                        cr.SplitType, cr.Leader, cr.IndentLevel, cr.LevelSeqNum, cr.SequenceString,
                                        cr.CritText));
                                }

                                study_iec_type = (crits.Count == 1) ? 2 : 4;
                                num_inc_criteria = crits.Count;
                            }
                        }

                        string ec = elig_statement[exc_pos..];
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
                                        cr.SplitType, cr.Leader, cr.IndentLevel, cr.LevelSeqNum, cr.SequenceString,
                                        cr.CritText));
                                }

                                study_iec_type += (crits.Count == 1) ? 5 : 6;
                            }
                        }
                    }
                }
                else if (elig_low.Contains("inclusion"))
                {
                    // Criteria listed for inclusion but no explicit exclusion criteria.

                    List<Criterion>? crits = IECFunctions.GetNumberedCriteria(sid, elig_statement, "inclusion");
                    if (crits is not null)
                    {
                        int seq_num = 0;
                        foreach (Criterion cr in crits)
                        {
                            seq_num++;
                            iec.Add(new StudyIEC(sid, seq_num, cr.CritTypeId, cr.CritType,
                                cr.SplitType, cr.Leader, cr.IndentLevel, cr.LevelSeqNum, cr.SequenceString,
                                cr.CritText));
                        }

                        study_iec_type = (crits.Count == 1) ? 1 : 2;
                    }
                }
                else if (elig_low.Contains("eligibility"))
                {
                    // May be a single list couched as 'eligibility' rather than 'inclusion'

                    List<Criterion>? crits = IECFunctions.GetNumberedCriteria(sid, elig_statement, "eligibility");
                    if (crits is not null)
                    {
                        int seq_num = 0;
                        foreach (Criterion cr in crits)
                        {
                            seq_num++;
                            iec.Add(new StudyIEC(sid, seq_num, cr.CritTypeId, cr.CritType,
                                cr.SplitType, cr.Leader, cr.IndentLevel, cr.LevelSeqNum, cr.SequenceString,
                                cr.CritText));
                        }

                        study_iec_type += (crits.Count == 1) ? 1 : 3;
                    }
                }
                else
                {
                    // Difficult to interpret - add as a single statement.

                    iec.Add(new StudyIEC(sid, 1, 3, "eligibility", "none", "All Elig",
                        0, 0, "0.AA", elig_statement));
                    study_iec_type = 1;
                }

            }

            s.iec_level = study_iec_type;
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Overall officials
        ///////////////////////////////////////////////////////////////////////////////////////

        if (ContactsLocationsModule is not null)
        {
            // now split into contacts, overall officials ....?

            var OverallOfficials = ContactsLocationsModule.overallOfficials;
            if (OverallOfficials?.Any() is true)
            {
                foreach (var official in OverallOfficials)
                {
                    string? official_name = official.name;
                    if (official_name is not null && official_name.AppearsGenuinePersonName())
                    {
                        official_name = official_name.TidyPersonName();
                        if (official_name != rp_name) // check not already present
                        {
                            string? official_affiliation = official.affiliation;
                            string? affil_organisation = null;
                            if (official_affiliation is not null
                                && official_affiliation.IsNotPlaceHolder()
                                && official_affiliation.AppearsGenuineOrgName())
                            {
                                official_affiliation = official_affiliation.TidyOrgName(sid);
                                if (!string.IsNullOrEmpty(sponsor_name)
                                    && official_affiliation!.ToLower().Contains(sponsor_name.ToLower()))
                                {
                                    affil_organisation = sponsor_name;
                                }
                                else
                                {
                                    affil_organisation = official_affiliation!.ExtractOrganisation(sid);
                                }
                            }

                            people.Add(new StudyPerson(sid, 51, "Study Lead",
                                official_name, official_affiliation, null, affil_organisation));
                        }
                    }
                }
            }


            ///////////////////////////////////////////////////////////////////////////////////////
            // Geographical locations
            ///////////////////////////////////////////////////////////////////////////////////////

            var locations = ContactsLocationsModule.locations;
            if (locations?.Any() is true)
            {
                foreach (var location in locations)
                {
                    string? facility = null;
                    string? fac = location.facility?.LineClean();
                    if (fac is not null)
                    {
                        // Common abbreviations used within CGT site descriptors

                        facility = fac.Replace(".", "");
                        facility = facility.Replace("Med ", "Medical ");
                        facility = facility.Replace("Gen ", "General ");
                        if (facility.EndsWith(" Univ") || facility.Contains("Univ "))
                        {
                            facility = facility.Replace("Univ", "University");
                        }

                        if (facility.EndsWith(" Ctr") || facility.Contains("Ctr "))
                        {
                            facility = facility.Replace("Ctr", "Center"); // N.r. US spelling
                        }

                        if (facility.EndsWith(" Hosp") || facility.Contains("Hosp "))
                        {
                            facility = facility.Replace("Hosp", "Hospital"); // N.r. US spelling
                        }
                    }

                    string? city = location.city;
                    string? country = location.country;
                    string? status = location.status;
                    int? status_id = string.IsNullOrEmpty(status) ? null : status.GetStatusId();
                    sites.Add(new StudyLocation(sid, facility, city, country, status_id, status));
                }
            }


            // derive distinct countries from sites

            if (sites.Any())
            {
                foreach (StudyLocation st in sites)
                {
                    if (st.country_name != null)
                    {
                        st.country_name = st.country_name.LineClean();
                        if (countries.Count == 0)
                        {
                            countries.Add(new StudyCountry(sid, st.country_name!));
                        }
                        else
                        {
                            bool add_country = true;
                            foreach (StudyCountry c in countries)
                            {
                                if (c.country_name == st.country_name)
                                {
                                    add_country = false;
                                    break;
                                }
                            }

                            if (add_country)
                            {
                                countries.Add(new StudyCountry(sid, st.country_name!));
                            }
                        }
                    }
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // IPD information
        ///////////////////////////////////////////////////////////////////////////////////////

        if (IPDSharingModule is not null)
        {
            string sharing_statement = "";
            string? IPDSharing = IPDSharingModule.ipdSharing;

            if (IPDSharing is not null)
            {
                sharing_statement = "IPD Sharing: " + IPDSharing.FullClean() + " (as of " + status_verified_date +
                                    ")";
            }

            string? IPDSharingDescription = IPDSharingModule.description;

            if (IPDSharingDescription is not null)
            {
                sharing_statement += "\nDescription: " + IPDSharingDescription.FullClean();

                string? IPDSharingTimeFrame = IPDSharingModule.timeFrame;
                if (!string.IsNullOrEmpty(IPDSharingTimeFrame))
                {
                    sharing_statement += "\nTime frame: " + IPDSharingTimeFrame.FullClean();
                }

                string? IPDSharingAccessCriteria = IPDSharingModule.accessCriteria;
                if (!string.IsNullOrEmpty(IPDSharingAccessCriteria))
                {
                    sharing_statement += "\nAccess Criteria: " + IPDSharingAccessCriteria.FullClean();
                }

                string? IPDSharingURL = IPDSharingModule.url;
                if (!string.IsNullOrEmpty(IPDSharingURL))
                {
                    sharing_statement += "\nURL: " + IPDSharingURL.FullClean();
                }

                var other_info_types = IPDSharingModule.infoTypes;
                if (other_info_types?.Any() is true)
                {
                    string itemList =
                        other_info_types.Aggregate("", (current, info_type) => current + ", " + info_type);
                    sharing_statement += "\nAdditional information available: " + itemList[1..].FullClean();
                }
            }

            // put data reference at end ?
            s.data_sharing_statement = sharing_statement;
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        // Linked Data Object Data
        ///////////////////////////////////////////////////////////////////////////////////////

        int object_type_id;
        string object_type = "";
        int title_type_id;
        string title_type;

        string title_base;

        // this used for specific additional objects from GSK
        string gsk_access_details = "Following receipt of a signed Data Sharing Agreement (DSA), ";
        gsk_access_details +=
            "researchers are provided access to anonymized patient-level data and supporting documentation in a ";
        gsk_access_details +=
            "secure data access system, known as the SAS Clinical Trial Data Transparency (CTDT) system. ";
        gsk_access_details +=
            " GSK may provide data directly to researchers where they are assured that the data will be secure";

        // this used for specific additional objects from Servier
        string servier_access_details =
            "Servier will provide anonymized patient-level and study-level clinical trial data in response to ";
        servier_access_details +=
            "scientifically valid research proposals. Qualified scientific or medical researchers can submit a research ";
        servier_access_details +=
            "proposal to Servier after registering on the site. If the request is approved and before the transfer of data, ";
        servier_access_details += "a so-called Data Sharing Agreement will have to be signed with Servier";

        // Set up initial registry entry data objects.
        // First establish base for title.

        if (brief_title is not null)
        {
            title_base = brief_title;
            title_type_id = 22;
            title_type = "Study short name :: object type";
        }
        else if (official_title is not null)
        {
            title_base = official_title;
            title_type_id = 24;
            title_type = "Study scientific name :: object type";
        }
        else
        {
            title_base = sid;
            title_type_id = 26;
            title_type = "Study registry ID :: object type";
        }


        // First object is the protocol registration
        // title - will be display title as well.

        string object_title = "CTG registry entry";
        string object_display_title = title_base + " :: CTG registry entry";

        // Define and provide initial values.
        // Create Id for the data object, then add title, dates and instance.

        string sd_oid = sid + " :: 13 :: " + object_title;

        data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, first_post?.year,
            23, "Text", 13, "Trial Registry entry", 100120,
            "ClinicalTrials.gov", 12, download_datetime));

        object_titles.Add(new ObjectTitle(sd_oid, object_display_title, title_type_id, title_type, true));

        if (first_post is not null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                first_post.year, first_post.month, first_post.day, first_post.date_string));
        }

        if (update_post is not null)
        {
            object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                update_post.year, update_post.month, update_post.day, update_post.date_string));
        }

        string url = "https://clinicaltrials.gov/ct2/show/study/" + sid;
        object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true,
            39, "Web text with XML or JSON via API"));

        // If present, set up results data object, with title, dates and instance.

        if (results_present)
        {
            object_title = "CTG results entry";
            object_display_title = title_base + " :: CTG results entry";
            sd_oid = sid + " :: 28 :: " + object_title;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, results_post?.year,
                23, "Text", 28, "Trial registry results summary", 100120,
                "ClinicalTrials.gov", 12, download_datetime));

            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                title_type_id, title_type, true));

            if (results_post is not null)
            {
                object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                    results_post.year, results_post.month, results_post.day, results_post.date_string));
            }

            if (update_post is not null)
            {
                object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                    update_post.year, update_post.month, update_post.day, update_post.date_string));
            }

            url = "https://clinicaltrials.gov/ct2/show/results/" + sid;
            object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true,
                39, "Web text with XML or JSON via API"));
        }


        if (LargeDocumentModule is not null)
        {
            var large_docs = LargeDocumentModule.largeDocs;
            if (large_docs?.Any() is true)
            {
                foreach (var largedoc in large_docs)
                {
                    string? type_abbrev = largedoc.typeAbbrev;
                    string? doc_label = largedoc.label;
                    string? doc_date = largedoc.date;
                    string? upload_date = largedoc.uploadDate;
                    string? file_name = largedoc.filename;

                    // Create a new data object,
                    // decompose the doc date (as the creation date)
                    // and the upload date (also used for the publication year).
                    // get the object type and type id from the type_abbrev.

                    SplitDate? docdate = null;
                    if (doc_date is not null)
                    {
                        docdate = doc_date.GetDatePartsFromISOString();
                    }

                    SplitDate? uploaddate = null;
                    if (upload_date != null)
                    {
                        uploaddate = upload_date[..10].GetDatePartsFromISOString();
                    }

                    if (type_abbrev is not null)
                    {
                        Tuple<int, string> doctype = type_abbrev switch
                        {
                            "Prot" => new Tuple<int, string>(11, "Study protocol"),
                            "SAP" => new Tuple<int, string>(22, "Statistical analysis plan"),
                            "ICF" => new Tuple<int, string>(18, "Informed consent forms"),
                            "Prot_SAP" => new Tuple<int, string>(74, "Protocol SAP"),
                            "Prot_ICF" => new Tuple<int, string>(75, "Protocol ICF"),
                            "Prot_SAP_ICF" => new Tuple<int, string>(76, "Protocol SAP ICF"),
                            _ => new Tuple<int, string>(37, type_abbrev),
                        };
                        object_type_id = doctype.Item1;
                        object_type = doctype.Item2;

                        // Title type depends on whether label is present.

                        if (!string.IsNullOrEmpty(doc_label))
                        {
                            object_display_title = title_base + " :: " + doc_label;
                            title_type_id = 21;
                            title_type = "Study short name :: object name";
                        }
                        else
                        {
                            object_display_title = title_base + " :: " + object_type;
                            title_type_id = 22;
                            title_type = "Study short name :: object type";
                        }

                        // Check here not a previous data object of the same type.
                        // It may have the same url. If so ignore it (to be implemented).
                        // If it appears to be different, add a suffix to the data object name

                        int next_num = CheckObjectName(object_titles, object_display_title);
                        if (next_num > 0)
                        {
                            object_display_title += "_" + next_num.ToString();
                        }

                        object_title = object_display_title.Substring(title_base.Length + 4);
                        sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                        data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title,
                            uploaddate?.year,
                            23, "Text", object_type_id, object_type, 100120, "ClinicalTrials.gov", 11,
                            download_datetime));

                        object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                            title_type_id, title_type, true));

                        if (docdate != null)
                        {
                            object_dates.Add(new ObjectDate(sd_oid, 15, "Created",
                                docdate.year, docdate.month, docdate.day, docdate.date_string));
                        }

                        if (upload_date != null)
                        {
                            object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                                uploaddate?.year, uploaddate?.month, uploaddate?.day, uploaddate?.date_string));
                        }

                        url = "https://clinicaltrials.gov/ProvidedDocs/" + sid[^2..] + "/" + sid + "/" + file_name;
                        object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true, 11,
                            "PDF"));
                    }
                }
            }
        }


        if (ReferencesModule != null)
        {
            // references cannot become data objects until
            // their dates are checked against the study date
            // this is therefore generating a list for the future.

            var refs = ReferencesModule.references;
            if (refs?.Any() is true)
            {
                foreach (var refce in refs)
                {
                    string? ref_type = refce.type;
                    if (ref_type is "result" or "derived")
                    {
                        string? pmid = refce.pmid;
                        string? citation = refce.citation.LineClean();
                        references.Add(new StudyReference(sid, pmid, citation, null, null));
                    }

                    var rets = refce.retractions;
                    if (rets?.Any() is true)
                    {
                        foreach (var ret in rets)
                        {
                            string? retraction_pmid = ret.pmid;
                            string? retraction_source = ret.source;
                            references.Add(new StudyReference(sid, retraction_pmid, retraction_source, null,
                                "RETRACTION"));
                        }
                    }
                }
            }


            // some of the available ipd may be transformable into data objects available, either
            // directly or after review of requests
            // Others will need to be stored as records for future processing

            var avail_ipd_items = ReferencesModule.availIpds;
            if (avail_ipd_items?.Any() is true)
            {
                foreach (var avail_ipd in avail_ipd_items)
                {
                    string? ipd_id = avail_ipd.id;
                    string? ipd_type = avail_ipd.type;
                    string? ipd_url = avail_ipd.url;
                    string? ipd_comment = avail_ipd.comment?.LineClean();
                    if (ipd_url is null) continue;

                    int object_class_id;
                    string object_class;

                    // Often a GSK store

                    if (ipd_url.Contains("clinicalstudydatarequest.com"))
                    {
                        if (ipd_type is not null)
                        {
                            Tuple<int, string> doctype = ipd_type switch
                            {
                                "Informed Consent Form" => new Tuple<int, string>(18, "Informed consent forms"),
                                "Dataset Specification" => new Tuple<int, string>(31, "Data dictionary"),
                                "Annotated Case Report Form" => new Tuple<int, string>(30,
                                    "Annotated data collection forms"),
                                "Statistical Analysis Plan" => new Tuple<int, string>(22,
                                    "Statistical analysis plan"),
                                "Individual Participant Data Set" => new Tuple<int, string>(80,
                                    "Individual participant data"),
                                "Clinical Study Report" => new Tuple<int, string>(26, "Clinical study report"),
                                "Study Protocol" => new Tuple<int, string>(11, "Study protocol"),
                                _ => new Tuple<int, string>(0, "")
                            };
                            object_type_id = doctype.Item1;
                            object_type = doctype.Item2;

                            if (object_type_id != 0)
                            {
                                object_class_id = (object_type_id == 80) ? 14 : 23;
                                object_class = (object_type_id == 80) ? "Dataset" : "Text";

                                int? sponsor_id;
                                string t_base;

                                if (sponsor_name is "GlaxoSmithKline" or "GSK")
                                {
                                    sponsor_id = 100163;
                                    t_base = "GSK-";
                                }
                                else
                                {
                                    sponsor_id = null;
                                    t_base = sponsor_name + "-";
                                }

                                if (ipd_id == null)
                                {
                                    t_base = title_base;
                                    title_type_id = 22;
                                    title_type = "Study short name :: object type";
                                }
                                else
                                {
                                    t_base += ipd_id;
                                    title_type_id = 20;
                                    title_type = "Unique data object title";
                                }

                                object_display_title = t_base + " :: " + object_type;

                                // check name
                                int next_num = CheckObjectName(object_titles, object_display_title);
                                if (next_num > 0)
                                {
                                    object_display_title += "_" + next_num.ToString();
                                }

                                object_title = object_display_title.Substring(t_base.Length + 4);

                                sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                                // add data object
                                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title,
                                    null,
                                    object_class_id, object_class, object_type_id, object_type, sponsor_id,
                                    sponsor_name,
                                    17, "Case by case download", gsk_access_details,
                                    "https://clinicalstudydatarequest.com/Help/Help-How-to-Request-Data.aspx",
                                    null, download_datetime));

                                // add in title
                                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                    title_type_id, title_type, true));

                                // for datasets also add dataset properties - even if they are largely unknown
                                if (object_type_id == 80)
                                {
                                    if (sponsor_name is "GlaxoSmithKline" or "GSK")
                                    {
                                        object_datasets.Add(new ObjectDataset(sd_oid,
                                            3, "Anonymised",
                                            "GSK states that... 'researchers are provided access to anonymized patient-level data '",
                                            2, "De-identification applied", null,
                                            0, "Not known", null));
                                    }
                                    else
                                    {
                                        object_datasets.Add(new ObjectDataset(sd_oid,
                                            0, "Not known", null,
                                            0, "Not known", null,
                                            0, "Not known", null));
                                    }
                                }
                            }
                            else
                            {
                                // store data for later inspection
                                ipd_info.Add(new AvailableIPD(sid, ipd_id, ipd_type, ipd_url, ipd_comment));
                            }
                        }
                    }

                    else if (ipd_url.Contains("servier.com"))
                    {
                        // Create a new data object.

                        if (ipd_type is not null)
                        {
                            if (ipd_type.ToLower().Contains("study-level clinical trial data"))
                            {
                                object_type_id = 69;
                                object_type = "Aggregated result dataset";
                            }
                            else
                            {
                                Tuple<int, string> doctype = ipd_type switch
                                {
                                    "Informed Consent Form" => new Tuple<int, string>(18, "Informed consent forms"),
                                    "Statistical Analysis Plan" => new Tuple<int, string>(22,
                                        "Statistical analysis plan"),
                                    "Individual Participant Data Set" => new Tuple<int, string>(80,
                                        "Individual participant data"),
                                    "Clinical Study Report" => new Tuple<int, string>(26, "Clinical study report"),
                                    "Study Protocol" => new Tuple<int, string>(11, "Study protocol"),
                                    "Dataset Specification" => new Tuple<int, string>(31, "Data dictionary"),
                                    "Annotated Case Report Form" => new Tuple<int, string>(30,
                                        "Annotated data collection forms"),
                                    _ => new Tuple<int, string>(0, "")
                                };

                                object_type_id = doctype.Item1;
                                object_type = doctype.Item2;
                            }

                            if (object_type_id != 0)
                            {
                                object_class_id = object_type_id is 80 or 69 ? 14 : 23;
                                object_class = object_type_id is 80 or 69 ? "Dataset" : "Text";

                                object_display_title = title_base + " :: " + object_type;

                                // check name
                                int next_num = CheckObjectName(object_titles, object_display_title);
                                if (next_num > 0)
                                {
                                    object_display_title += "_" + next_num.ToString();
                                }

                                object_title = object_display_title.Substring(title_base.Length + 4);

                                sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title,
                                    null,
                                    object_class_id, object_class, object_type_id, object_type, 101418, "Servier",
                                    18, "Case by case on-screen access", servier_access_details,
                                    "https://clinicaltrials.servier.com/data-request-portal/", null,
                                    download_datetime));

                                // add in title
                                title_type_id = 22;
                                title_type = "Study short name :: object type";
                                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                    title_type_id, title_type, true));

                                if (object_type_id == 80)
                                {
                                    object_datasets.Add(new ObjectDataset(sd_oid,
                                        3, "Anonymised",
                                        "Servier states that... 'Servier will provide anonymized patient-level and study-level clinical trial data'",
                                        2, "De-identification applied", null,
                                        0, "Not known", null));
                                }
                            }
                        }
                        else
                        {
                            // Store data for later inspection.

                            ipd_info.Add(new AvailableIPD(sid, ipd_id, ipd_type, ipd_url, ipd_comment));
                        }
                    }

                    else if (ipd_url.Contains("merck.com"))
                    {
                        // Some of the merck records are direct access to a page
                        // with a further link to a pdf, plus other study components.
                        // The others are indications that the object exists but is not directly available.
                        // create a new data object.

                        if (ipd_url.Contains("&tab=access"))
                        {
                            object_type_id = 79;
                            object_type = "CSR summary";
                            object_class_id = 23;
                            object_class = "Text";

                            object_display_title = title_base + " :: " + object_type;

                            // Check name before adding object and object attributes.

                            int next_num = CheckObjectName(object_titles, object_display_title);
                            if (next_num > 0)
                            {
                                object_display_title += "_" + next_num.ToString();
                            }

                            object_title = object_display_title.Substring(title_base.Length + 4);

                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                object_class_id, object_class, object_type_id, object_type,
                                100165, "Merck Sharp & Dohme", 11, download_datetime));

                            title_type_id = 22;
                            title_type = "Study short name :: object type";
                            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                title_type_id, title_type, true));

                            object_instances.Add(new ObjectInstance(sd_oid, 100165,
                                "Merck Sharp & Dohme Corp.", ipd_url, true, 11, "PDF", null, null));
                        }
                        else
                        {
                            // store data for later inspection
                            ipd_info.Add(new AvailableIPD(sid, ipd_id, ipd_type, ipd_url, ipd_comment));
                        }
                    }

                    else if (ipd_url.Contains("biolincc"))
                    {
                        // do nothing - these objects should be picked up
                        // by the biolincc extraction process
                    }

                    else if (ipd_url.Contains("immport") || ipd_url.Contains("itntrialshare")
                                                         || ipd_url.Contains("drive.google") ||
                                                         ipd_url.Contains("zenodo")
                                                         || ipd_url.Contains("dataverse") ||
                                                         ipd_url.Contains("datadryad")
                                                         || ipd_url.Contains("github") || ipd_url.Contains("osf.io")
                                                         || ipd_url.Contains("scribd") ||
                                                         ipd_url.Contains("researchgate"))
                    {
                        // these sites seem to have available data objects with specific URLs

                        string? ipd_type_lower = ipd_type?.ToLower();
                        if (ipd_type_lower is not null && ipd_type_lower.StartsWith("study") &&
                            (ipd_type_lower.Contains("design") || ipd_type_lower.Contains("details")
                                                               || ipd_type_lower.Contains("overview") ||
                                                               ipd_type_lower.Contains("summary")
                                                               || ipd_type_lower.Contains("synopsis")))
                        {
                            ipd_type_lower = "study summary";
                        }

                        if (ipd_type_lower is not null
                            && ipd_type_lower.StartsWith("complete set of descriptive data"))
                        {
                            ipd_type_lower = "study summary";
                        }

                        Tuple<int, string> doctype = ipd_type_lower switch
                        {
                            "study protocol" => new Tuple<int, string>(11, "Study protocol"),
                            "individual participant data set" => new Tuple<int, string>(80,
                                "Statistical analysis plan"),
                            "clinical study report" => new Tuple<int, string>(26, "Clinical study report"),
                            "informed consent form" => new Tuple<int, string>(18, "Informed consent forms"),
                            "study forms" => new Tuple<int, string>(21, "Data collection forms"),
                            "statistical analysis plan" => new Tuple<int, string>(22, "Statistical analysis plan"),
                            "manual of procedure" => new Tuple<int, string>(36, "Manual of procedures"),
                            "analytic code" => new Tuple<int, string>(29, "Analysis notes"),
                            "study summary" => new Tuple<int, string>(38, "Study overview"),
                            "data coding manuals" => new Tuple<int, string>(82, "Data coding manual"),
                            "questionnaire" => new Tuple<int, string>(40, "Standard instruments"),
                            _ => new Tuple<int, string>(0, "")
                        };

                        object_type_id = doctype.Item1;
                        object_type = doctype.Item2;

                        // used as defaults if not over-written by best guesses.

                        int resource_type_id = 0;
                        string resource_type = "Not yet known";
                        if (object_type_id is 11 or 26 or 18 or 22 or 36 or 82)
                        {
                            resource_type_id = 11;
                            resource_type = "PDF";
                        }

                        if (object_type_id != 0)
                        {
                            object_class_id = (object_type_id == 80) ? 14 : 23;
                            object_class = (object_type_id == 80) ? "Dataset" : "Text";

                            if (string.IsNullOrEmpty(ipd_id))
                            {
                                object_display_title = title_base + " :: " + object_type;
                                title_type_id = 22;
                                title_type = "Study short name :: object type";
                            }
                            else
                            {
                                string object_name = ipd_type + " (" + ipd_id + ")";
                                object_display_title = title_base + " :: " + object_name;
                                title_type_id = 21;
                                title_type = "Study short name :: object name";
                            }

                            // check name
                            int next_num = CheckObjectName(object_titles, object_display_title);
                            if (next_num > 0)
                            {
                                object_display_title += "_" + next_num.ToString();
                            }

                            object_title = object_display_title.Substring(title_base.Length + 4);
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                object_class_id, object_class, object_type_id, object_type, null, sponsor_name,
                                11, download_datetime));

                            // add in title
                            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                title_type_id, title_type, true));

                            // for datasets also add dataset properties - even if they are largely unknown
                            if (object_type_id == 80)
                            {
                                object_datasets.Add(new ObjectDataset(sd_oid,
                                    0, "Not known", null,
                                    0, "Not known", null,
                                    0, "Not known", null));
                            }

                            string? repo_org_name = ipd_url switch
                            {
                                _ when ipd_url.Contains("immport") => "Immport",
                                _ when ipd_url.Contains("itntrialshare") => "Immune Tolerance Network",
                                _ when ipd_url.Contains("drive.google") => "Google Drive",
                                _ when ipd_url.Contains("dataverse") => "Dataverse",
                                _ when ipd_url.Contains("datadryad") => "Datadryad",
                                _ when ipd_url.Contains("github") => "GitHub",
                                _ when ipd_url.Contains("osf.io") => "Open Science Foundation",
                                _ when ipd_url.Contains("scribd") => "Scribd",
                                _ when ipd_url.Contains("researchgate") => "Research Gate",
                                _ when ipd_url.Contains("zenodo") => "Zenodo",
                                _ => null
                            };

                            // add in instance
                            object_instances.Add(new ObjectInstance(sd_oid, null, repo_org_name, ipd_url, true,
                                resource_type_id, resource_type));
                        }
                        else
                        {
                            // store data for later inspection
                            ipd_info.Add(new AvailableIPD(sid, ipd_id, ipd_type, ipd_url, ipd_comment));
                        }
                    }
                    else
                    {
                        // store data for later inspection
                        // N.B. pubmed and other references that are marked as protocols need to be processed somehow...

                        ipd_info.Add(new AvailableIPD(sid, ipd_id, ipd_type, ipd_url, ipd_comment));
                    }
                }
            }


            // at the moment these records are mainly for storage and future processing.
            // Tidy up urls, remove a small proportion of obvious non-useful links

            var see_also_refs = ReferencesModule.seeAlsoLinks;
            if (see_also_refs?.Any() is true)
            {
                foreach (var see_also_ref in see_also_refs)
                {
                    string? link_label = see_also_ref.label;
                    if (link_label is not null)
                    {
                        link_label = link_label.Trim(' ', '|', '.', ':', '\"');
                        if (link_label.StartsWith('(') && link_label.EndsWith(')'))
                        {
                            link_label = link_label.Trim('(', ')');
                        }

                        link_label = link_label.LineClean();
                    }

                    string? link_url = see_also_ref.url;
                    if (link_url is not null)
                    {
                        link_url = link_url.Trim(' ', '/');
                    }

                    if (!string.IsNullOrEmpty(link_url))
                    {
                        bool add_to_links_table = true;
                        if (link_label == "NIH Clinical Center Detailed Web Page" && link_url.EndsWith(".html"))
                        {
                            // add new data object
                            // disregard the other entries - as they lead nowhere.

                            object_type_id = 38;
                            object_type = "Study Overview";
                            int object_class_id = 23;
                            string object_class = "Text";

                            object_display_title = title_base + " :: " + object_type;

                            // check name
                            int next_num = CheckObjectName(object_titles, object_display_title);
                            if (next_num > 0)
                            {
                                object_display_title += "_" + next_num.ToString();
                            }

                            object_title = object_display_title.Substring(title_base.Length + 4);
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                object_class_id, object_class, object_type_id, object_type, 100360,
                                "National Institutes of Health Clinical Center", 11, download_datetime));

                            // add in title
                            title_type_id = 22;
                            title_type = "Study short name :: object type";
                            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                title_type_id, title_type, true));

                            // add in instance
                            object_instances.Add(new ObjectInstance(sd_oid, 100360,
                                "National Institutes of Health Clinical Center",
                                link_url, true, 35, "Web text"));

                            add_to_links_table = false;
                        }

                        if (link_url.Contains("filehosting.pharmacm.com/Download"))
                        {
                            string test_url = link_url.ToLower();
                            object_type_id = 0;

                            if (test_url.Contains("csr") ||
                                (test_url.Contains("study") && test_url.Contains("report")))
                            {
                                if (test_url.Contains("redacted"))
                                {
                                    object_type_id = 27;
                                    object_type = "Redacted Clinical Study Report";
                                }
                                else if (test_url.Contains("summary"))
                                {
                                    object_type_id = 79;
                                    object_type = "CSR Summary";
                                }
                                else
                                {
                                    object_type_id = 26;
                                    object_type = "Clinical Study Report";
                                }
                            }

                            else if (test_url.Contains("csp") || test_url.Contains("protocol"))
                            {
                                if (test_url.Contains("redacted"))
                                {
                                    object_type_id = 42;
                                    object_type = "Redacted Protocol";
                                }
                                else
                                {
                                    object_type_id = 11;
                                    object_type = "Study Protocol";
                                }
                            }

                            else if (test_url.Contains("sap") || test_url.Contains("analysis"))
                            {
                                if (test_url.Contains("redacted"))
                                {
                                    object_type_id = 43;
                                    object_type = "Redacted SAP";
                                }
                                else
                                {
                                    object_type_id = 22;
                                    object_type = "Statistical analysis plan";
                                }
                            }

                            else if (test_url.Contains("summary") || test_url.Contains("rds"))
                            {
                                object_type_id = 79;
                                object_type = "CSR summary";
                            }

                            else if (test_url.Contains("poster"))
                            {
                                object_type_id = 108;
                                object_type = "Conference Poster";
                            }


                            if (object_type_id > 0 && sponsor_name != null)
                            {
                                // Probably need to add a new data object. By default....

                                object_display_title = title_base + " :: " + object_type;

                                // check here not a previous data object of the same type
                                // It may have the same url. If so ignore it.
                                // If it appears to be different, add a suffix to the data object name.

                                if (!CheckDuplicateUrl(object_instances, link_url))
                                {
                                    int next_num = CheckObjectName(object_titles, object_display_title);
                                    if (next_num > 0)
                                    {
                                        object_display_title += "_" + next_num.ToString();
                                    }

                                    object_title = object_display_title.Substring(title_base.Length + 4);

                                    sd_oid = sid + " :: " + object_type_id + " :: " + object_title;

                                    DataObject doc_object = new DataObject(sd_oid, sid, object_title,
                                        object_display_title, null,
                                        23, "Text", object_type_id, object_type, null, sponsor_name, 11,
                                        download_datetime);

                                    // add data object
                                    data_objects.Add(doc_object);

                                    // add in title
                                    title_type_id = 22;
                                    title_type = "Study short name :: object type";
                                    object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                        title_type_id, title_type, true));

                                    // add in instance
                                    object_instances.Add(new ObjectInstance(sd_oid, 101419, "TrialScope Disclose",
                                        link_url, true, 11, "PDF", null, null));

                                    add_to_links_table = false;
                                }
                            }
                        }

                        // Disregard the following links, as they are not worth further inspection.

                        if (link_label == "To obtain contact information for a study center near you, click here.")
                            add_to_links_table = false;
                        if (link_label ==
                            "Researchers can use this site to request access to anonymised patient level data and/or supporting documents from clinical studies to conduct further research.")
                            add_to_links_table = false;
                        if (link_label == "University of Texas MD Anderson Cancer Center Website")
                            add_to_links_table = false;
                        if (link_label == "UT MD Anderson Cancer Center website") add_to_links_table = false;
                        if (link_label == "Clinical Trials at Novo Nordisk") add_to_links_table = false;
                        if (link_label == "Memorial Sloan Kettering Cancer Center") add_to_links_table = false;
                        if (link_label == "AmgenTrials clinical trials website") add_to_links_table = false;
                        if (link_label == "Mayo Clinic Clinical Trials") add_to_links_table = false;
                        if (link_url == "http://trials.boehringer-ingelheim.com") add_to_links_table = false;
                        if (string.IsNullOrEmpty(link_label) &&
                            (link_url.EndsWith(".com") || link_url.EndsWith(".org"))) add_to_links_table = false;

                        // only add to links table if all tests above have failed, for possible further inspection.

                        if (add_to_links_table && !string.IsNullOrEmpty(link_label))
                        {
                            studylinks.Add(new StudyLink(sid, link_label, link_url));
                        }
                    }
                }
            }
        }


        // Edit contributors - try to ensure properly categorised
        // check if a group inserted as an individual, and then
        // check if an individual added as a group.

        List<StudyPerson> people2 = new();
        if (people.Count > 0)
        {
            bool add = true;
            foreach (StudyPerson p in people)
            {
                string? full_name = p.person_full_name?.ToLower();
                if (full_name is not null && !full_name.AppearsGenuinePersonName())
                {
                    string? organisation_name = p.person_full_name.TidyOrgName(sid);
                    if (organisation_name is not null)
                    {
                        organisations.Add(new StudyOrganisation(sid, p.contrib_type_id, p.contrib_type,
                            null, organisation_name));
                        add = false;
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
                if (org_name is not null && !org_name.AppearsGenuineOrgName())
                {
                    string? person_full_name = g.organisation_name.TidyPersonName();
                    if (person_full_name is not null)
                    {
                        people2.Add(new StudyPerson(sid, g.contrib_type_id, g.contrib_type, person_full_name,
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

        List<StudyPerson> people3 = new();

        // try to identify repeated individuals...
        // can happen as people are put in under different categories

        int n = 0;
        foreach (StudyPerson p2 in people2)
        {
            bool add_person = true;
            n++;
            if (n > 1)
            {
                foreach (StudyPerson p3 in people3)
                {
                    if (p2.person_full_name?.ToLower() == p3.person_full_name?.ToLower())
                    {
                        add_person = false;

                        // but retain this info if needed / possible

                        if (string.IsNullOrEmpty(p3.person_affiliation)
                            && !string.IsNullOrEmpty(p2.person_affiliation))
                        {
                            p3.person_affiliation = p2.person_affiliation;
                        }

                        if (string.IsNullOrEmpty(p3.organisation_name)
                            && !string.IsNullOrEmpty(p2.organisation_name))
                        {
                            p3.organisation_name = p2.organisation_name;
                        }

                        break;
                    }
                }
            }

            if (add_person)
            {
                people3.Add(p2);
            }
        }

        s.identifiers = identifiers;
        s.titles = titles;
        s.people = people3;
        s.organisations = orgs2;
        s.references = references;
        s.studylinks = studylinks;
        s.ipd_info = ipd_info;
        s.topics = topics;
        s.features = features;
        s.relationships = relationships;
        s.sites = sites;
        s.countries = countries;
        s.conditions = conditions;
        s.iec = iec;

        s.data_objects = data_objects;
        s.object_datasets = object_datasets;
        s.object_titles = object_titles;
        s.object_dates = object_dates;
        s.object_instances = object_instances;

        return s;
    }

  
    // check name...
    private int CheckObjectName(List<ObjectTitle> titles, string object_display_title)
    {
        int num_of_this_type = 0;
        if (titles.Count > 0)
        {
            foreach (var t in titles)
            {
                string? title_to_test = t.title_text;
                if (title_to_test is not null)
                {
                    if (title_to_test.Contains(object_display_title))
                    {
                        num_of_this_type++;
                    }
                }
            }
        }
        return num_of_this_type;
    }

    // Check URL not a duplicate
    private bool CheckDuplicateUrl(List<ObjectInstance> instances, string url)
    {
        bool url_already_present = false;
        if (instances.Count > 0)
        {
            foreach (var inst in instances)
            {
                string? url_to_test = inst.url;
                if (url_to_test is not null)
                {
                    if (url_to_test == url)
                    {
                        url_already_present = true;
                        break;
                    }
                }
            }
        }
        return url_already_present;
    }

}