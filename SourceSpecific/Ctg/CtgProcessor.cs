using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MDR_Harvester.Extensions;

namespace MDR_Harvester.Ctg;

public class CTGProcessor : IStudyProcessor
{
    public Study? ProcessData(string json_string, DateTime? download_datetime, ILoggingHelper _logging_helper)
    {
        // set up json reader and deserialise file to a ISRCTN_Record object.

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        CTG_Record? r = JsonSerializer.Deserialize<CTG_Record?>(json_string, json_options);
        if (r is null)
        {
            _logging_helper.LogError($"Unable to deserialise json file to Ctg_Record\n{json_string[..1000]}... (first 1000 characters)");
            return null;
        }
        
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

        CTGHelpers ih = new();

        Protocolsection? ps = r.ProtocolSection;
        Identificationmodule? IdentificationModule = ps?.IdentificationModule;
        Statusmodule? StatusModule = ps?.StatusModule;

        if (IdentificationModule is null || StatusModule is null)
        {
            _logging_helper.LogError($"No valid Identification or Status module found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        string sid = IdentificationModule.NCTId!;

        if (string.IsNullOrEmpty(sid))
        {
            _logging_helper.LogError($"No valid study identifier found for study\n{json_string[..1000]}... (first 1000 characters of json string");
            return null;
        }

        s.sd_sid = sid;
        s.datetime_of_data_fetch = download_datetime;

        Sponsorcollaboratorsmodule? SponsorCollaboratorsModule = ps?.SponsorCollaboratorsModule;
        Descriptionmodule? DescriptionModule = ps?.DescriptionModule;
        Conditionsmodule? ConditionsModule = ps?.ConditionsModule;
        Designmodule? DesignModule = ps?.DesignModule;
        Eligibilitymodule? EligibilityModule = ps?.EligibilityModule;
        Contactslocationsmodule? ContactsLocationsModule = ps?.ContactsLocationsModule;
        Referencesmodule? ReferencesModule = ps?.ReferencesModule;
        Ipdsharingstatementmodule? IPDSharingModule = ps?.IPDSharingStatementModule;

        Documentsection? d = r.DocumentSection;
        Largedocumentmodule? LargeDocumentModule = d?.LargeDocumentModule;

        Derivedsection? v =  r.DerivedSection;
        Conditionbrowsemodule? ConditionBrowseModule  = v?.ConditionBrowseModule;
        Interventionbrowsemodule? InterventionBrowseModule = v?.InterventionBrowseModule;

        string? brief_title = IdentificationModule.BriefTitle?.ReplaceApos()?.Trim();
        string? official_title = IdentificationModule.OfficialTitle?.ReplaceApos()?.Trim();
        string? acronym = IdentificationModule.Acronym?.Trim();
                
        // This date is a simple field in the status module
        // assumed to be the date the identifier was assigned.

        string? submissionDate = StatusModule.StudyFirstSubmitDate;

        // add the NCT identifier record - 100120 is the id of ClinicalTrials.gov.

        submissionDate = submissionDate.StandardiseCTGDateFormat();
        identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100120,
                                    "ClinicalTrials.gov", submissionDate, null));

        // Add title records.


        if (!string.IsNullOrEmpty(brief_title))
        {
            titles.Add(new StudyTitle(sid, brief_title, 15, "Registry public title", true, "From Clinicaltrials.gov"));
            s.display_title = brief_title;

            if (!string.IsNullOrEmpty(official_title) && official_title.ToLower() != brief_title.ToLower())
            {
                titles.Add(new StudyTitle(sid, official_title, 16, "Registry scientific title", false, "From Clinicaltrials.gov"));
            }
            if (!string.IsNullOrEmpty(acronym) && !string.IsNullOrEmpty(official_title)
                    && acronym.ToLower() != brief_title.ToLower()
                    && acronym.ToLower() != official_title.ToLower())
            {
                titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From Clinicaltrials.gov"));
            }
        }
        else
        {
            // No Brief Title.

            if (!string.IsNullOrEmpty(official_title))
            {
                titles.Add(new StudyTitle(sid, official_title, 16, "Registry scientific title", true, "From Clinicaltrials.gov"));
                s.display_title = official_title;

                if (!string.IsNullOrEmpty(acronym) && acronym.ToLower() != official_title.ToLower())
                {
                    titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From Clinicaltrials.gov"));
                }
            }
            else
            {
                // Only an acronym present (very rare).

                titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", true, "From Clinicaltrials.gov"));
                s.display_title = acronym;
            }
        }

        // Get the sponsor id information. The sponsor name is in the organization field
        // while the OrgStudyId Info has details on the identifier itself (value = org_study_id)
        // and its type, any link and 'domain' - may be the organisation name
        // (records seems to use either the domain or the org full name). 

        string? org = IdentificationModule.Organization?.OrgFullName?.TidyOrgName(sid); 
        string? org_study_id = IdentificationModule.OrgStudyIdInfo?.OrgStudyId;
        string? org_id_type = IdentificationModule.OrgStudyIdInfo?.OrgStudyIdType;
        string? org_id_domain = IdentificationModule.OrgStudyIdInfo?.OrgStudyIdDomain?.TidyOrgName(sid);
        string? org_id_link = IdentificationModule.OrgStudyIdInfo?.OrgStudyIdLink;

        // add the sponsor's identifier.

        if (org_study_id is not null)
        {
            // (Rarely, and wrongly, people put the same name in both org and org_study_id fields...
            // i.e. they put the identifier value also in as the organisation name).
            
            bool add_id = !(org is not null 
                            && String.Equals(org, org_study_id, StringComparison.CurrentCultureIgnoreCase));

            if (add_id)
            {
                if (org_id_type == "U.S. NIH Grant/Contract")
                {
                    // A funder Id rather than a sponsor's Id.

                    identifiers.Add(new StudyIdentifier(sid, org_study_id, 13, "Funder’s ID",
                        100134, "National Institutes of Health", null, org_id_link));
                }
                else
                {
                    if (org == "[Redacted]")
                    {
                        string org_name = "(sponsor name redacted in registry record)";
                        identifiers.Add(new StudyIdentifier(sid, org_study_id, 14, "Sponsor’s ID",
                            13, org_name, null, null));
                    }
                    else
                    {
                        string? org_name = (!string.IsNullOrEmpty(org)) ? org : org_id_domain;
                        identifiers.Add(new StudyIdentifier(sid, org_study_id, 14, "Sponsor’s ID",
                            null, org_name, null, org_id_link));
                    }
                }
            }
        }


        // add any additional identifiers (if not already used as a sponsor id).

        var secIds = IdentificationModule.SecondaryIdInfoList?.SecondaryIdInfo;
        if (secIds?.Any() is true)
        { 
            foreach (var sec_id in secIds)
            {
                string? id_value = sec_id.SecondaryId; 
                string? id_link = sec_id.SecondaryIdLink;

                // Check not already used as the sponsor id (or there is no sponsor id, =org_study_id)
                if (id_value is not null)
                {
                    if (string.IsNullOrEmpty(org_study_id) || (id_value.Trim().ToLower() != org_study_id.Trim().ToLower()))
                    {
                        string? identifier_type = sec_id.SecondaryIdType;
                        string? identifier_org = sec_id.SecondaryIdDomain?.TidyOrgName(sid);

                        // Deduce as much as possible about the secondary id, using its value, type and org.

                        IdentifierDetails idd = ih.GetCTGIdentifierProps(identifier_type, identifier_org, id_value);

                        // Add the secondary identifier
                        identifiers.Add(new StudyIdentifier(sid, idd.id_value, idd.id_type_id, idd.id_type,
                                                        idd.id_org_id, idd.id_org, null, id_link));
                    }
                }
            }
        }


        // Also add any NCT aliases (obsolete Ids).

        var obsoleteIds = IdentificationModule.NCTIdAliasList?.NCTIdAlias;
        if (obsoleteIds?.Any() is true)
        {
            foreach (string obsId in obsoleteIds)
            {
                identifiers.Add(new StudyIdentifier(sid, obsId, 44, "Obsolete NCT number",
                                                        100120, "ClinicalTrials.gov", null, null));
            }
        }


        // Get the main registry entry dates if they are available.

        SplitDate? first_post = null;
        SplitDate? results_post = null;
        SplitDate? update_post = null;

        var FirstPostDate = StatusModule.StudyFirstPostDateStruct;
        if (FirstPostDate is not null)
        {
            string? first_post_type = FirstPostDate.StudyFirstPostDateType;
            if (first_post_type is "Actual" or "Estimate")
            {
                first_post = FirstPostDate.StudyFirstPostDate?.GetDatePartsFromCTGString();
                if (first_post is not null && first_post_type == "Estimate")
                {
                    first_post.date_string += " (est.)";
                }
            }
        }

        bool results_present = false;

        var ResultsPostDate = StatusModule.ResultsFirstPostDateStruct;
        if (ResultsPostDate is not null)
        {
            string? results_type = ResultsPostDate.ResultsFirstPostDateType;
            if (results_type is not null &&
                results_type is "Actual" or "Estimate")
            {
                results_post = ResultsPostDate.ResultsFirstPostDate?.GetDatePartsFromCTGString();
                if (results_post is not null && results_type == "Estimate")
                {
                    results_post.date_string += " (est.)";
                }

                // Assumption is that if results are available the results post
                // must be present with an actual or estimated (not anticipated) date.
                        
                results_present = true;
            }
        }

        var LastUpdateDate = StatusModule.LastUpdatePostDateStruct;
        if (LastUpdateDate is not null)
        {
            string? update_type = LastUpdateDate.LastUpdatePostDateType;
            if (update_type is not null &&
                update_type is "Actual" or "Estimate")
            {
                update_post = LastUpdateDate.LastUpdatePostDate?.GetDatePartsFromCTGString();
                if (update_post is not null && update_type == "Estimate")
                {
                    update_post.date_string += " (est.)";
                }
            }
        }

        // expanded access details
        var ExpandedAccessInfo = StatusModule.ExpandedAccessInfo;
        if (ExpandedAccessInfo is not null)
        {
            string? expanded_access_nct_id = ExpandedAccessInfo.ExpandedAccessNCTId?.Trim();
            if (expanded_access_nct_id != null)
            {
                relationships.Add(new StudyRelationship(sid, 23, "has an expanded access version", expanded_access_nct_id));
                relationships.Add(new StudyRelationship(expanded_access_nct_id, 24, "is an expanded access version of", sid));
            }
        }
                   
        // get and store study start date, if available
        var StudyStartDate = StatusModule.StartDateStruct;
        if (StudyStartDate != null)
        {
            SplitDate? start_date = StudyStartDate.StartDate?.GetDatePartsFromCTGString();
            s.study_start_year = start_date?.year;
            s.study_start_month = start_date?.month;
        }

        s.study_status = StatusModule.OverallStatus;
        s.study_status_id = s.study_status.GetStatusId();
        string? status_verified_date = StatusModule.StatusVerifiedDate;


        string? rp_name = "";   // responsible party's name - define here to allow later comparison
        string? sponsor_name = null;  // defined here to allow later comparison

        if (SponsorCollaboratorsModule != null)
        {
            var sponsor = SponsorCollaboratorsModule.LeadSponsor;
            if (sponsor is not null)
            {
                string? sponsor_candidate = sponsor.LeadSponsorName;
                if (sponsor_candidate.AppearsGenuineOrgName())
                {
                    sponsor_name = sponsor_candidate.TidyOrgName(sid);
                    if (sponsor_name == "[Redacted]")
                    {
                        sponsor_name = "(sponsor name redacted in registry record)";
                    }
                    organisations.Add(new StudyOrganisation(sid, 54, "Trial Sponsor", null, sponsor_name));
                }
            }

            var resp_party = SponsorCollaboratorsModule.ResponsibleParty;
            if (resp_party is not null)
            {
                string? rp_type = resp_party.ResponsiblePartyType;
                if (rp_type != "Sponsor")
                {
                    rp_name = resp_party.ResponsiblePartyInvestigatorFullName;
                    string? rp_affil = resp_party.ResponsiblePartyInvestigatorAffiliation;

                    string? rp_old_name_title = resp_party.ResponsiblePartyOldNameTitle;
                    string? rp_old_org = resp_party.ResponsiblePartyOldOrganization;

                    if (string.IsNullOrEmpty(rp_name) && !string.IsNullOrEmpty(rp_old_name_title))
                    {
                        rp_name = rp_old_name_title;
                    }

                    if (string.IsNullOrEmpty(rp_affil) && !string.IsNullOrEmpty(rp_old_org))
                    {
                        rp_affil = rp_old_org;
                    }

                    if (!string.IsNullOrEmpty(rp_name) && rp_name != "[Redacted]")
                    {
                        if (rp_name.CheckPersonName())
                        {
                            rp_name = rp_name.TidyPersonName();
                            if (rp_name != "")
                            {
                                string? affil_organisation = null;
                                if (!rp_affil.AppearsGenuineOrgName())
                                {
                                    rp_affil = null;
                                }

                                if (!string.IsNullOrEmpty(rp_affil))
                                {
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

                                if (rp_type == "Sponsor-Investigator")
                                {
                                    people.Add(new StudyPerson(sid, 70, "Sponsor-investigator",
                                                    rp_name, rp_affil, null, affil_organisation));
                                }
                            }
                        }
                    }
                }
            }

            var collaboratorList = SponsorCollaboratorsModule.CollaboratorList;
            if (collaboratorList is not null)
            {
                var Collaborators = collaboratorList.Collaborator;
                if (Collaborators?.Any() is true)
                {
                    foreach (var col in Collaborators)
                    {
                        string? collab_candidate = col.CollaboratorName;
                        if (collab_candidate.AppearsGenuineOrgName())
                        {
                            string? collab_name = collab_candidate?.TidyOrgName(sid);
                            organisations.Add(new StudyOrganisation(sid, 69, "Collaborating organisation", null, collab_name));
                        }
                    }
                }
            }

        }


        if (DescriptionModule != null)
        {
            // CTG descriptions do not seem to include tags, but to be safe....

            s.brief_description = DescriptionModule.BriefSummary.StringClean();
        }


        if (ConditionBrowseModule != null)
        {
            var condition_mesh_list = ConditionBrowseModule.ConditionMeshList;
            if (condition_mesh_list is not null)
            {
                var conds = condition_mesh_list.ConditionMesh;
                if (conds?.Any() is true)
                {
                    foreach (var con in conds)
                    {
                        string? mesh_code = con.ConditionMeshId;
                        string? mesh_term = con.ConditionMeshTerm;
                        topics.Add(new StudyTopic(sid, 13, "condition", true, mesh_code, mesh_term));
                    }
                }
            }
        }


        if (InterventionBrowseModule != null)
        {
            var intervention_mesh_list = InterventionBrowseModule.InterventionMeshList;
            if (intervention_mesh_list is not null)
            {
                var interventions = intervention_mesh_list.InterventionMesh;
                {
                    if (interventions?.Any() is true)
                    {
                        foreach (var interv in interventions)
                        {
                            string? mesh_code = interv.InterventionMeshId;
                            string? mesh_term = interv.InterventionMeshTerm;
                            topics.Add(new StudyTopic(sid, 12, "chemical / agent", true, mesh_code, mesh_term));
                        }
                    }
                }
            }
        }


        if (ConditionsModule != null)
        {
            var conditions_list = ConditionsModule.ConditionList;
            if (conditions_list is not null)
            {
                var conds = conditions_list.Condition;
                if (conds?.Any() is true)
                {
                    foreach (string condition in conds)
                    {
                        // only add the condition name if not already present in the mesh coded conditions.

                        if (topic_is_new(condition))
                        {
                            topics.Add(new StudyTopic(sid, 13, "condition", condition));
                        }
                    }
                }
            }

            var keywords_list = ConditionsModule.KeywordList;
            if (keywords_list is not null)
            {
                var keywords = keywords_list.Keyword;
                if (keywords?.Any() is true)
                {
                    foreach (string keyword in keywords)
                    {
                        // Regularise drug name
                        string k_word = keyword;   // need to do this indirectly as cannot alter the foreach variable
                        if (k_word.Contains(((char)174).ToString()))
                        {
                            k_word = k_word.Replace(((char)174).ToString(), "");    // drop reg mark
                            k_word = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(k_word.ToLower());
                        }

                        // only add the condition name if not already present in the mesh coded conditions
                        if (topic_is_new(k_word))
                        {
                            topics.Add(new StudyTopic(sid, 11, "keyword", k_word));
                        }
                    }
                }
            }
        }


        bool topic_is_new(string candidate_topic)
        {
            foreach (StudyTopic k in topics)
            {
                if (k.original_value!.ToLower() == candidate_topic.ToLower())
                {
                    return false;
                }
            }
            return true;
        }


        if (DesignModule != null)
        {
            s.study_type = DesignModule.StudyType;
            s.study_type_id = s.study_type.GetTypeId();

            if (s.study_type == "Interventional")
            {
                var Phaselist = DesignModule.PhaseList;
                if (Phaselist is not null)
                {
                    var phases = Phaselist.Phase;
                    if (phases?.Any() is true)
                    {
                        foreach (string phase in phases)
                        {
                            features.Add(new StudyFeature(sid, 20, "phase", phase.GetPhaseId(), phase));
                        }
                    }
                }
                else
                {
                    string phase = "Not provided";
                    features.Add(new StudyFeature(sid, 20, "phase", phase.GetPhaseId(), phase));
                }


                var design_info = DesignModule.DesignInfo;
                if (design_info is not null)
                {
                    string design_allocation = design_info.DesignAllocation ?? "Not provided";
                    features.Add(new StudyFeature(sid, 22, "allocation type", design_allocation.GetAllocationTypeId(), design_allocation));

                    string design_intervention_model = design_info.DesignInterventionModel ?? "Not provided";
                    features.Add(new StudyFeature(sid, 23, "intervention model", design_intervention_model.GetDesignTypeId(), design_intervention_model));

                    string design_primary_purpose = design_info.DesignPrimaryPurpose ?? "Not provided";
                    features.Add(new StudyFeature(sid, 21, "primary purpose", design_primary_purpose.GetPrimaryPurposeId(), design_primary_purpose));

                    var masking_details = design_info.DesignMaskingInfo;
                    string design_masking = "Not provided";      // default value             
                    if (masking_details != null)
                    {
                        design_masking = masking_details.DesignMasking ?? "Not provided";
                    }
                    features.Add(new StudyFeature(sid, 24, "masking", design_masking.GetMaskingTypeId(), design_masking));
                }
            }


            if (s.study_type == "Observational")
            {
                string? patient_registry = DesignModule.PatientRegistry;
                if (patient_registry == "Yes")  // change type...
                {
                    s.study_type_id = 13;
                    s.study_type = "Observational Patient Registry";
                }

                var design_info = DesignModule.DesignInfo;
                if (design_info is not null)
                {
                    var obs_model_list = design_info.DesignObservationalModelList;
                    if (obs_model_list is not null)
                    {
                        var obs_models = obs_model_list.DesignObservationalModel;
                        if (obs_models?.Any() is true)
                        {
                            foreach (string obs_model in obs_models)
                            {
                                features.Add(new StudyFeature(sid, 30, "observational model", 
                                    obs_model.GetObsModelTypeId(), obs_model));
                            }
                        }
                    }
                    else
                    {
                        string obs_model = "Not provided";
                        features.Add(new StudyFeature(sid, 30, "observational model",
                            obs_model.GetObsModelTypeId(), obs_model));
                    }

                    var time_persp_list = design_info.DesignTimePerspectiveList;
                    if (time_persp_list is not null)
                    {
                        var time_persps = time_persp_list.DesignTimePerspective;
                        if (time_persps?.Any() is true)
                        {
                            foreach (string time_persp in time_persps)
                            {
                                features.Add(new StudyFeature(sid, 31, "time perspective", 
                                    time_persp.GetTimePerspectiveId(), time_persp));
                            }
                        }
                    }
                    else
                    {
                        string time_persp = "Not provided";
                        features.Add(new StudyFeature(sid, 31, "time perspective",
                            time_persp.GetTimePerspectiveId(), time_persp));
                    }
                }

                var biospec_details = DesignModule.BioSpec;
                if (biospec_details is not null)
                {
                    string biospec_retention = biospec_details.BioSpecRetention ?? "Not provided";
                    features.Add(new StudyFeature(sid, 32, "biospecimens retained", 
                        biospec_retention.GetSpecimenRetentionId(), biospec_retention));
                }
            }

            var enrol_details = DesignModule.EnrollmentInfo;
            if (enrol_details is not null)
            {
                string? enrolment_count = enrol_details.EnrollmentCount;
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


        if (EligibilityModule != null)
        {
            s.study_gender_elig = EligibilityModule.Gender ?? "Not provided";
            if (s.study_gender_elig == "All")
            {
                s.study_gender_elig = "Both";
            }
            s.study_gender_elig_id = s.study_gender_elig.GetGenderEligId();

            string? min_age = EligibilityModule.MinimumAge;
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

            string? max_age = EligibilityModule.MaximumAge;
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
        }


        if (ContactsLocationsModule != null)
        {
            var officials = ContactsLocationsModule.OverallOfficialList;
            if (officials is not null)
            {
                var OverallOfficials = officials.OverallOfficial;
                if (OverallOfficials?.Any() is true)
                {
                    foreach (var official in OverallOfficials)
                    {
                        string? official_name = official.OverallOfficialName;
                        if (official_name is not null && official_name.CheckPersonName())
                        {
                            official_name = official_name.TidyPersonName();
                            if (official_name != rp_name)     // check not already present
                            {
                                string? official_affiliation = official.OverallOfficialAffiliation;
                                string? affil_organisation = null;
                                if (official_affiliation is not null && official_affiliation.AppearsGenuineOrgName())
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
            }


            var Locationlist = ContactsLocationsModule.LocationList;
            if (Locationlist is not null)
            {
                var locations = Locationlist.Location;
                if (locations?.Any() is true)
                {
                    foreach (var location in locations)
                    {
                        string? facility = null;
                        string? fac = location.LocationFacility?.TrimPlus()?.ReplaceApos();
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
                                facility = facility.Replace("Ctr", "Center");  // N.r. US spelling
                            }
                            if (facility.EndsWith(" Hosp") || facility.Contains("Hosp "))
                            {
                                facility = facility.Replace("Hosp", "Hospital");  // N.r. US spelling
                            }
                        }
                        string? city = location.LocationCity;
                        string? country = location.LocationCountry;
                        string? status = location.LocationStatus;
                        int? status_id = string.IsNullOrEmpty(status) ? null : status.GetStatusId();
                        sites.Add(new StudyLocation(sid, facility, city, country, status_id, status));
                    }
                }
            }

            // derive distinct countries from sites

            if (sites.Any())
            {
                foreach (StudyLocation st in sites)
                {
                    if (st.country_name != null)
                    {
                        st.country_name = st.country_name.Trim().ReplaceApos();
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

        // ipd information

        if (IPDSharingModule != null)
        {
            string sharing_statement = "";
            string? IPDSharing = IPDSharingModule.IPDSharing;

            if (IPDSharing is not null)
            {
                sharing_statement = "IPD Sharing: " + IPDSharing.StringClean() + " (as of " + status_verified_date + ")";
            }

            string? IPDSharingDescription = IPDSharingModule.IPDSharingDescription;

            if (IPDSharingDescription is not null)
            {
                sharing_statement += "\nDescription: " + IPDSharingDescription.StringClean();

                string? IPDSharingTimeFrame = IPDSharingModule.IPDSharingTimeFrame;
                if (!string.IsNullOrEmpty(IPDSharingTimeFrame))
                {
                    sharing_statement += "\nTime frame: " + IPDSharingTimeFrame.StringClean();
                }

                string? IPDSharingAccessCriteria = IPDSharingModule.IPDSharingAccessCriteria;
                if (!string.IsNullOrEmpty(IPDSharingAccessCriteria))
                {
                    sharing_statement += "\nAccess Criteria: " + IPDSharingAccessCriteria.StringClean();
                }

                string? IPDSharingURL = IPDSharingModule.IPDSharingURL;
                if (!string.IsNullOrEmpty(IPDSharingURL))
                {
                    sharing_statement += "\nURL: " + IPDSharingURL.StringClean();
                }

                var IPDSharingInfoTypeList = IPDSharingModule.IPDSharingInfoTypeList;
                if (IPDSharingInfoTypeList is not null)
                {
                    var other_info_types = IPDSharingInfoTypeList.IPDSharingInfoType;
                    if (other_info_types?.Any() is true)
                    {   
                        string itemList = other_info_types.Aggregate("", (current, info_type) => current + ", " + info_type);
                        sharing_statement += "\nAdditional information available: " + itemList[1..].StringClean();
                    }
                }

            }

            s.data_sharing_statement = sharing_statement;
        }


        /********************* Linked Data Object Data **********************************/

        int object_type_id;
        string object_type = "";
        int title_type_id;
        string title_type;

        string title_base;

        // this used for specific additional objects from GSK
        string gsk_access_details = "Following receipt of a signed Data Sharing Agreement (DSA), ";
        gsk_access_details += "researchers are provided access to anonymized patient-level data and supporting documentation in a ";
        gsk_access_details += "secure data access system, known as the SAS Clinical Trial Data Transparency (CTDT) system. ";
        gsk_access_details += " GSK may provide data directly to researchers where they are assured that the data will be secure";

        // this used for specific additional objects from Servier
        string servier_access_details = "Servier will provide anonymized patient-level and study-level clinical trial data in response to ";
        servier_access_details += "scientifically valid research proposals. Qualified scientific or medical researchers can submit a research ";
        servier_access_details += "proposal to Servier after registering on the site. If the request is approved and before the transfer of data, ";
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
            var large_doc_list = LargeDocumentModule.LargeDocList;
            if (large_doc_list is not null)
            {
                var large_docs = large_doc_list.LargeDoc;
                if (large_docs?.Any() is true)
                {
                    foreach (var largedoc in large_docs)
                    {
                        string? type_abbrev = largedoc.LargeDocTypeAbbrev;
                        string? doc_label = largedoc.LargeDocLabel;
                        string? doc_date = largedoc.LargeDocDate;
                        string? upload_date = largedoc.LargeDocUploadDate;
                        string? file_name = largedoc.LargeDocFilename;

                        // Create a new data object,
                        // decompose the doc date (as the creation date)
                        // and the upload date (also used for the publication year).
                        // get the object type and type id from the type_abbrev.

                        SplitDate? docdate = null;
                        if (doc_date is not null)
                        {
                            docdate = doc_date.GetDatePartsFromCTGString();
                        }
                        SplitDate? uploaddate = null;
                        if (upload_date != null)
                        {
                            // Machine generated - uses mm/dd/yyyy time format.

                            uploaddate = upload_date[..10].GetDatePartsFromUSString();
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
                                title_type_id = 21; title_type = "Study short name :: object name";
                            }
                            else
                            {
                                object_display_title = title_base + " :: " + object_type;
                                title_type_id = 22; title_type = "Study short name :: object type";
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

                            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, uploaddate?.year,
                            23, "Text", object_type_id, object_type, 100120, "ClinicalTrials.gov", 11, download_datetime));

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
                            object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true, 11, "PDF"));
                        }
                    }
                }
            }
        }


        if (ReferencesModule != null)
        {
            // references cannot become data objects until
            // their dates are checked against the study date
            // this is therefore generating a list for the future.

            var refList = ReferencesModule.ReferenceList;
            if (refList is not null)
            {
                var refs = refList.Reference;
                if (refs?.Any() is true)
                {
                    foreach (var refce in refs)
                    {
                        string? ref_type = refce.ReferenceType;
                        if (ref_type is "result" or "derived")
                        {
                            string? pmid = refce.ReferencePMID;
                            string? citation = refce.ReferenceCitation.ReplaceApos();
                            references.Add(new StudyReference(sid, pmid, citation, null, null));
                        }

                        var retractionList = refce.RetractionList;
                        if (retractionList is not null)
                        {
                            var rets = retractionList.Retraction;
                            if (rets?.Any() is true)
                            {
                                foreach (var ret in rets)
                                {
                                    string? retraction_pmid = ret.RetractionPMID;
                                    string? retraction_source = ret.RetractionSource;
                                    references.Add(new StudyReference(sid, retraction_pmid, retraction_source, null, "RETRACTION"));
                                }
                            }
                        }
                    }
                }
            }


            // some of the available ipd may be transformable into data objects available, either
            // directly or after review of requests
            // Others will need to be stored as records for future processing

            var avail_ipd_list = ReferencesModule.AvailIPDList;
            int object_class_id;
            string object_class;
            if (avail_ipd_list is not null)
            {
                var avail_ipd_items = avail_ipd_list.AvailIPD;
                if (avail_ipd_items?.Any() is true)
                {
                    foreach (var avail_ipd in avail_ipd_items)
                    {
                        string? ipd_id = avail_ipd.AvailIPDId;
                        string? ipd_type = avail_ipd.AvailIPDType;
                        string? ipd_url = avail_ipd.AvailIPDURL;
                        string? ipd_comment = avail_ipd.AvailIPDComment?.ReplaceApos();
                        if (ipd_url is null) continue;
                        
                        // Often a GSK store

                        if (ipd_url.Contains("clinicalstudydatarequest.com"))
                        {
                            if (ipd_type is not null)
                            {
                                Tuple<int, string> doctype = ipd_type switch
                                {
                                    "Informed Consent Form" => new Tuple<int, string>(18, "Informed consent forms"),
                                    "Dataset Specification" => new Tuple<int, string>(31, "Data dictionary"),
                                    "Annotated Case Report Form" => new Tuple<int, string>(30, "Annotated data collection forms"),
                                    "Statistical Analysis Plan" => new Tuple<int, string>(22, "Statistical analysis plan"),
                                    "Individual Participant Data Set" => new Tuple<int, string>(80, "Individual participant data"),
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
                                        title_type_id = 22; title_type = "Study short name :: object type";
                                    }
                                    else
                                    {
                                        t_base += ipd_id;
                                        title_type_id = 20; title_type = "Unique data object title";
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
                                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                        object_class_id, object_class, object_type_id, object_type, sponsor_id, sponsor_name,
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
                                                3, "Anonymised", "GSK states that... 'researchers are provided access to anonymized patient-level data '",
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
                                    object_type_id = 69; object_type = "Aggregated result dataset";
                                }
                                else
                                {
                                    Tuple<int, string> doctype = ipd_type switch
                                    {
                                        "Informed Consent Form" => new Tuple<int, string>(18, "Informed consent forms"),
                                        "Statistical Analysis Plan" => new Tuple<int, string>(22, "Statistical analysis plan"),
                                        "Individual Participant Data Set" => new Tuple<int, string>(80, "Individual participant data"),
                                        "Clinical Study Report" => new Tuple<int, string>(26, "Clinical study report"),
                                        "Study Protocol" => new Tuple<int, string>(11, "Study protocol"),
                                        "Dataset Specification" => new Tuple<int, string>(31, "Data dictionary"),
                                        "Annotated Case Report Form" => new Tuple<int, string>(30, "Annotated data collection forms"),
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

                                    data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                        object_class_id, object_class, object_type_id, object_type, 101418, "Servier",
                                        18, "Case by case on-screen access", servier_access_details,
                                        "https://clinicaltrials.servier.com/data-request-portal/", null, download_datetime));

                                    // add in title
                                    title_type_id = 22; title_type = "Study short name :: object type";
                                    object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                        title_type_id, title_type, true));

                                    if (object_type_id == 80)
                                    {
                                        object_datasets.Add(new ObjectDataset(sd_oid,
                                            3, "Anonymised", "Servier states that... 'Servier will provide anonymized patient-level and study-level clinical trial data'",
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
                                object_type_id = 79; object_type = "CSR summary";
                                object_class_id = 23; object_class = "Text";

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

                                title_type_id = 22; title_type = "Study short name :: object type";
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
                                || ipd_url.Contains("drive.google") || ipd_url.Contains("zenodo")
                                || ipd_url.Contains("dataverse") || ipd_url.Contains("datadryad")
                                || ipd_url.Contains("github") || ipd_url.Contains("osf.io")
                                || ipd_url.Contains("scribd") || ipd_url.Contains("researchgate"))
                        {
                            // these sites seem to have available data objects with specific URLs

                            string? ipd_type_lower = ipd_type?.ToLower();
                            if (ipd_type_lower is not null && ipd_type_lower.StartsWith("study") &&
                                (ipd_type_lower.Contains("design") || ipd_type_lower.Contains("details")
                                                                   || ipd_type_lower.Contains("overview") || ipd_type_lower.Contains("summary")
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
                                "individual participant data set" => new Tuple<int, string>(80, "Statistical analysis plan"),
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
            }


            // at the moment these records are mainly for storage and future processing.
            // Tidy up urls, remove a small proportion of obvious non-useful links

            var see_also_refs_list = ReferencesModule.SeeAlsoLinkList;
            if (see_also_refs_list is not null)
            {
                var see_also_refs = see_also_refs_list.SeeAlsoLink;
                if (see_also_refs?.Any() is true)
                {
                    foreach (var see_also_ref in see_also_refs)
                    {
                        string? link_label = see_also_ref.SeeAlsoLinkLabel;
                        if (link_label is not null)
                        {
                            link_label = link_label.Trim(' ', '|', '.', ':', '\"');
                            if (link_label.StartsWith('(') && link_label.EndsWith(')'))
                            {
                                link_label = link_label.Trim('(', ')');
                            }
                            link_label = link_label.ReplaceApos();
                        }

                        string? link_url = see_also_ref.SeeAlsoLinkURL;
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

                                object_type_id = 38; object_type = "Study Overview";
                                object_class_id = 23; object_class = "Text";

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
                                title_type_id = 22; title_type = "Study short name :: object type";
                                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                title_type_id, title_type, true));

                                // add in instance
                                object_instances.Add(new ObjectInstance(sd_oid, 100360, "National Institutes of Health Clinical Center",
                                            link_url, true, 35, "Web text"));

                                add_to_links_table = false;
                            }

                            if (link_url.Contains("filehosting.pharmacm.com/Download"))
                            {
                                string test_url = link_url.ToLower();
                                object_type_id = 0;

                                if (test_url.Contains("csr") || (test_url.Contains("study") && test_url.Contains("report")))
                                {
                                    if (test_url.Contains("redacted"))
                                    {
                                        object_type_id = 27; object_type = "Redacted Clinical Study Report";
                                    }
                                    else if (test_url.Contains("summary"))
                                    {
                                        object_type_id = 79; object_type = "CSR Summary";
                                    }
                                    else
                                    {
                                        object_type_id = 26; object_type = "Clinical Study Report";
                                    }
                                }

                                else if (test_url.Contains("csp") || test_url.Contains("protocol"))
                                {
                                    if (test_url.Contains("redacted"))
                                    {
                                        object_type_id = 42; object_type = "Redacted Protocol";
                                    }
                                    else
                                    {
                                        object_type_id = 11; object_type = "Study Protocol";
                                    }
                                }

                                else if (test_url.Contains("sap") || test_url.Contains("analysis"))
                                {
                                    if (test_url.Contains("redacted"))
                                    {
                                        object_type_id = 43; object_type = "Redacted SAP";
                                    }
                                    else
                                    {
                                        object_type_id = 22; object_type = "Statistical analysis plan";
                                    }
                                }

                                else if (test_url.Contains("summary") || test_url.Contains("rds"))
                                {
                                    object_type_id = 79; object_type = "CSR summary";
                                }

                                else if (test_url.Contains("poster"))
                                {
                                    object_type_id = 108; object_type = "Conference Poster";
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

                                        DataObject doc_object = new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                        23, "Text", object_type_id, object_type, null, sponsor_name, 11, download_datetime);

                                        // add data object
                                        data_objects.Add(doc_object);

                                        // add in title
                                        title_type_id = 22; title_type = "Study short name :: object type";
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

                            if (link_label == "To obtain contact information for a study center near you, click here.") add_to_links_table = false;
                            if (link_label == "Researchers can use this site to request access to anonymised patient level data and/or supporting documents from clinical studies to conduct further research.") add_to_links_table = false;
                            if (link_label == "University of Texas MD Anderson Cancer Center Website") add_to_links_table = false;
                            if (link_label == "UT MD Anderson Cancer Center website") add_to_links_table = false;
                            if (link_label == "Clinical Trials at Novo Nordisk") add_to_links_table = false;
                            if (link_label == "Memorial Sloan Kettering Cancer Center") add_to_links_table = false;
                            if (link_label == "AmgenTrials clinical trials website") add_to_links_table = false;
                            if (link_label == "Mayo Clinic Clinical Trials") add_to_links_table = false;
                            if (link_url == "http://trials.boehringer-ingelheim.com") add_to_links_table = false;
                            if (string.IsNullOrEmpty(link_label) && (link_url.EndsWith(".com") || link_url.EndsWith(".org"))) add_to_links_table = false;

                            // only add to links table if all tests above have failed, for possible further inspection.

                            if (add_to_links_table && !string.IsNullOrEmpty(link_label))
                            {
                                studylinks.Add(new StudyLink(sid, link_label, link_url));
                            }
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
                if (full_name is not null && full_name.IsAnOrganisation())
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
                if (org_name is not null && org_name.IsAnIndividual())
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