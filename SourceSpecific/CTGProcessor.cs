using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DataHarvester.ctg
{
    public class CTGProcessor : IStudyProcessor
    {
        IMonitorDataLayer _mon_repo;
        LoggingHelper _logger;

        public CTGProcessor(IMonitorDataLayer mon_repo, LoggingHelper logger)
        {
            _mon_repo = mon_repo;
            _logger = logger;
        }


        public Study ProcessData(XmlDocument d, DateTime? download_datetime)
        {
            //FullStudy fs = (FullStudy)rs;
            Study s = new Study();
            List<StudyIdentifier> identifiers = new List<StudyIdentifier>();
            List<StudyTitle> titles = new List<StudyTitle>();
            List<StudyContributor> contributors = new List<StudyContributor>();
            List<StudyContributor> contributors2 = new List<StudyContributor>();
            List<StudyReference> references = new List<StudyReference>();
            List<StudyLink> studylinks = new List<StudyLink>();
            List<AvailableIPD> ipd_info = new List<AvailableIPD>();
            List<StudyTopic> topics = new List<StudyTopic>();
            List<StudyFeature> features = new List<StudyFeature>();
            List<StudyRelationship> relationships = new List<StudyRelationship>();
            List<StudyLocation> sites = new List<StudyLocation>();
            List<StudyCountry> countries = new List<StudyCountry>();

            List<DataObject> data_objects = new List<DataObject>();
            List<ObjectDataset> object_datasets = new List<ObjectDataset>();
            List<ObjectTitle> object_titles = new List<ObjectTitle>();
            List<ObjectDate> object_dates = new List<ObjectDate>();
            List<ObjectInstance> object_instances = new List<ObjectInstance>();

            string sid = null;
            string submissionDate = null;
            string official_title = null;
            string acronym = null;
            string brief_title = null;
            string status_verified_date = null;
            bool results_data_present = false;
            string sponsor_name = null;
            SplitDate firstpost = null, resultspost = null, updatepost = null, startdate = null;

            StringHelpers sh = new StringHelpers(_logger);
            DateHelpers dh = new DateHelpers();
            TypeHelpers th = new TypeHelpers();
            MD5Helpers hh = new MD5Helpers();
            IdentifierHelpers ih = new IdentifierHelpers();
           
            XElement IdentificationModule = null;
            XElement StatusModule = null;
            XElement SponsorCollaboratorsModule = null;
            XElement DescriptionModule = null;
            XElement ConditionsModule = null;
            XElement DesignModule = null;
            XElement EligibilityModule = null;
            XElement ContactsLocationsModule = null;
            XElement ReferencesModule = null;
            XElement IPDSharingModule = null;
            XElement LargeDocumentModule = null;
            XElement ConditionBrowseModule = null;
            XElement InterventionBrowseModule = null;

            // First convert the XML document to a Linq XML Document.

            XDocument xDoc = XDocument.Load(new XmlNodeReader(d));

            // Obtain the main top level elements of the registry entry.

            XElement FullStudy = xDoc.Root;
            XElement Study = FullStudy.Element("Struct");
            IEnumerable<XElement> StudyTopSections = Study.Elements("Struct");

            XElement ProtocolSection = RetrieveStruct(Study, "ProtocolSection");
            if (ProtocolSection!= null)
            {
                IdentificationModule = RetrieveStruct(ProtocolSection, "IdentificationModule");
                StatusModule = RetrieveStruct(ProtocolSection, "StatusModule");
                SponsorCollaboratorsModule = RetrieveStruct(ProtocolSection, "SponsorCollaboratorsModule");
                DescriptionModule = RetrieveStruct(ProtocolSection, "DescriptionModule");
                ConditionsModule = RetrieveStruct(ProtocolSection, "ConditionsModule");
                DesignModule = RetrieveStruct(ProtocolSection, "DesignModule");
                EligibilityModule = RetrieveStruct(ProtocolSection, "EligibilityModule");
                ContactsLocationsModule = RetrieveStruct(ProtocolSection, "ContactsLocationsModule");
                ReferencesModule = RetrieveStruct(ProtocolSection, "ReferencesModule");
                IPDSharingModule = RetrieveStruct(ProtocolSection, "IPDSharingStatementModule");
            }


            XElement ResultsSection = RetrieveStruct(Study, "ResultsSection");
            if (ResultsSection != null)
            {
                bool ParticipantFlowModuleExists = CheckStructExists(ResultsSection, "ParticipantFlowModule");
                bool BaselineCharacteristicsModuleExists = CheckStructExists(ResultsSection, "BaselineCharacteristicsModule");
                bool OutcomeMeasuresModuleExists = CheckStructExists(ResultsSection, "OutcomeMeasuresModules");
                results_data_present = (ParticipantFlowModuleExists || BaselineCharacteristicsModuleExists
                    || OutcomeMeasuresModuleExists);
            }


            XElement DocumentSection = RetrieveStruct(Study, "DocumentSection");
            if (DocumentSection != null)
            {
                LargeDocumentModule = RetrieveStruct(DocumentSection, "LargeDocumentModule");
            }


            XElement DerivedSection = RetrieveStruct(Study, "DerivedSection");
            if (DerivedSection != null)
            {
                ConditionBrowseModule = RetrieveStruct(DerivedSection, "ConditionBrowseModule");
                InterventionBrowseModule = RetrieveStruct(DerivedSection, "InterventionBrowseModule");
            }


            // these two modules considered together, as both are fundamental,
            // and related study data structures require data from both modules
            if (IdentificationModule != null && StatusModule != null)
            {
                //var id_items = IdentificationModule.Items;
                //var status_items = StatusModule.Items;
                sid = FieldValue(IdentificationModule, "NCTId");
                s.sd_sid = sid;
                s.datetime_of_data_fetch = download_datetime;

                s.study_status = FieldValue(StatusModule, "OverallStatus");
                s.study_status_id = th.GetStatusId(s.study_status);
                status_verified_date = FieldValue(StatusModule, "StatusVerifiedDate");

                // this date is a simple field in the status module
                // assumed to be the date the identifier was assigned
                submissionDate = FieldValue(StatusModule, "StudyFirstSubmitDate");

                // add the NCT identifier record - 100120 is the id of ClinicalTrials.gov
                submissionDate = dh.StandardiseCTGDateFormat(submissionDate);
                identifiers.Add(new StudyIdentifier(sid, sid, 11, "Trial Registry ID", 100120,
                                            "ClinicalTrials.gov", submissionDate, null));

                // add title records

                brief_title = sh.ReplaceApos(FieldValue(IdentificationModule, "BriefTitle"))?.Trim() ?? "";
                official_title = sh.ReplaceApos(FieldValue(IdentificationModule, "OfficialTitle"))?.Trim() ?? ""; 
                acronym = FieldValue(IdentificationModule, "Acronym")?.Trim() ?? ""; 

                if (brief_title != "")
                {
                    titles.Add(new StudyTitle(sid, brief_title, 15, "Registry public title", true, "From Clinicaltrials.gov"));
                    s.display_title = brief_title;

                    if (official_title != "" && official_title.ToLower() != brief_title.ToLower())
                    {
                        titles.Add(new StudyTitle(sid, official_title, 16, "Registry scientific title", false, "From Clinicaltrials.gov"));
                    }
                    if (acronym != ""
                            && acronym.ToLower() != brief_title.ToLower()
                            && acronym.ToLower() != official_title.ToLower())
                    {
                        titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From Clinicaltrials.gov"));
                    }
                }
                else
                {
                    // No Brief Title

                    if (official_title != null)
                    {
                        titles.Add(new StudyTitle(sid, official_title, 16, "Registry scientific title", true, "From Clinicaltrials.gov"));
                        s.display_title = official_title;

                        if (acronym != null && acronym.ToLower() != official_title.ToLower())
                        {
                            titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", false, "From Clinicaltrials.gov"));
                        }
                    }
                    else
                    {
                        titles.Add(new StudyTitle(sid, acronym, 14, "Acronym or Abbreviation", true, "From Clinicaltrials.gov"));
                        s.display_title = acronym;
                    }
                }
            

                // get the sponsor id information
                string org = sh.TidyOrgName(StructFieldValue(IdentificationModule, "Organization", "OrgFullName") ?? "", sid);
                string org_study_id = StructFieldValue(IdentificationModule, "OrgStudyIdInfo", "OrgStudyId") ?? "";
                string org_id_type = StructFieldValue(IdentificationModule, "OrgStudyIdInfo", "OrgStudyIdType") ?? "";
                string org_id_domain = sh.TidyOrgName(StructFieldValue(IdentificationModule, "OrgStudyIdInfo", "OrgStudyIdDomain") ?? "", sid);
                string org_id_link = StructFieldValue(IdentificationModule, "OrgStudyIdInfo", "OrgStudyIdLink");

                // add the sponsor's identifier
                if (org_study_id.ToLower() != org.ToLower())
                {
                    // (Rarely, people put the same name in both org and org_study_id fields...)

                    if (org_id_type == "U.S. NIH Grant/Contract")
                    {
                        identifiers.Add(new StudyIdentifier(sid, org_study_id,
                                                13, "Funder’s ID", 100134, "National Institutes of Health",
                                                null, org_id_link));
                    }
                    else
                    {
                        if (org == "[Redacted]")
                        {
                            string org_name = "(sponsor name redacted in registry record)";
                            identifiers.Add(new StudyIdentifier(sid, org_study_id,
                                    14, "Sponsor’s ID", 13, org_name, null, null));
                        }
                        else
                        {
                            string org_name = (org_id_domain != "") ? org_id_domain : org;
                            identifiers.Add(new StudyIdentifier(sid, org_study_id,
                                    14, "Sponsor’s ID", null, org_name,
                                    null, org_id_link));
                        }
                    }
                }


                // add any additional identifiers (if not already used as a sponsor id)

                var secIds = RetrieveListElements(IdentificationModule, "SecondaryIdInfoList");
                if (secIds != null)
                {
                    foreach (XElement id_element in secIds)
                    {
                        string id_value = FieldValue(id_element, "SecondaryId");
                        string id_link = FieldValue(id_element, "SecondaryIdLink");
                        if (org_study_id == "" || id_value.Trim().ToLower() != org_study_id.Trim().ToLower())
                        {
                            string identifier_type = FieldValue(id_element, "SecondaryIdType");
                            string identifier_org = sh.TidyOrgName(FieldValue(id_element, "SecondaryIdDomain"), sid);
                            IdentifierDetails idd = ih.GetIdentifierProps(identifier_type, identifier_org, id_value);

                            // add the secondary identifier
                            identifiers.Add(new StudyIdentifier(sid, idd.id_value, idd.id_type_id, idd.id_type,
                                                            idd.id_org_id, idd.id_org, null, id_link));
                        }
                    }
                }


                // get the main three registry entry dates if they are available
                XElement FirstPostDate = RetrieveStruct(StatusModule, "StudyFirstPostDateStruct");
                if (FirstPostDate != null)
                {
                    string firstpost_type = FieldValue(FirstPostDate, "StudyFirstPostDateType");
                    if (firstpost_type != "Anticipated")
                    {
                        string firstpost_date = FieldValue(FirstPostDate, "StudyFirstPostDate");
                        firstpost = dh.GetDatePartsFromCTGString(firstpost_date);
                        if (firstpost_type.ToLower() == "estimate") firstpost.date_string += " (est.)";
                    }
                }

                XElement ResultsPostDate = RetrieveStruct(StatusModule, "ResultsFirstPostDateStruct");
                if (ResultsPostDate != null)
                {
                    string results_type = FieldValue(ResultsPostDate, "ResultsFirstPostDateType");
                    if (results_type != "Anticipated")
                    {
                        string resultspost_date = FieldValue(ResultsPostDate, "ResultsFirstPostDate");
                        resultspost = dh.GetDatePartsFromCTGString(resultspost_date);
                        if (results_type.ToLower() == "estimate") resultspost.date_string += " (est.)";
                    }
                }

                XElement LastUpdateDate = RetrieveStruct(StatusModule, "LastUpdatePostDateStruct");
                if (LastUpdateDate != null)
                {
                    string update_type = FieldValue(LastUpdateDate, "LastUpdatePostDateType");
                    if (update_type != "Anticipated")
                    {
                        string updatepost_date = FieldValue(LastUpdateDate, "LastUpdatePostDate");
                        updatepost = dh.GetDatePartsFromCTGString(updatepost_date);
                        if (update_type.ToLower() == "estimate") updatepost.date_string += " (est.)";
                    }
                }

                // expanded access details
                string expanded_access_nctid = StructFieldValue(StatusModule, "ExpandedAccessInfo", "ExpandedAccessNCTId");
                if (expanded_access_nctid != null)
                {
                    relationships.Add(new StudyRelationship(sid, 23, "has an expanded access version", expanded_access_nctid));
                    relationships.Add(new StudyRelationship(expanded_access_nctid, 24, "is an expanded access version of", sid));
                }


                // get and store study start date, if available, to use to check possible linked papers
                XElement StudyStartDate = RetrieveStruct(StatusModule, "StartDateStruct");
                if (StudyStartDate != null)
                {
                    string studystart_date = FieldValue(StudyStartDate, "StartDate");
                    startdate = dh.GetDatePartsFromCTGString(studystart_date);
                    s.study_start_year = startdate.year;
                    s.study_start_month = startdate.month;
                }

            }
            else
            {
                return null;  // something very odd - this data is basic
            }


            string rp_name = "";   // responsible party's name - define here to allow later comparison
            
            if (SponsorCollaboratorsModule != null)
            {
                XElement sponsor = RetrieveStruct(SponsorCollaboratorsModule, "LeadSponsor");
                if (sponsor != null)
                {
                    string sponsor_candidate = FieldValue(sponsor, "LeadSponsorName");
                    if (sh.AppearsGenuineOrgName(sponsor_candidate))
                    {
                        sponsor_name = sh.TidyOrgName(sponsor_candidate, sid);
                        if (sponsor_name == "[Redacted]") sponsor_name = "(sponsor name redacted in registry record)";

                        contributors.Add(new StudyContributor(sid, 54, "Trial Sponsor", null, sponsor_name));
                    }
                }

                
                XElement resp_party = RetrieveStruct(SponsorCollaboratorsModule, "ResponsibleParty");
                if (resp_party != null)
                {
                    string rp_type = FieldValue(resp_party, "ResponsiblePartyType");

                    if (rp_type != "Sponsor")
                    {
                        rp_name = FieldValue(resp_party, "ResponsiblePartyInvestigatorFullName") ?? "";
                        string rp_affil = FieldValue(resp_party, "ResponsiblePartyInvestigatorAffiliation");
                        string rp_oldnametitle = FieldValue(resp_party, "ResponsiblePartyOldNameTitle") ?? "";
                        string rp_oldorg = FieldValue(resp_party, "ResponsiblePartyOldOrganization");

                        if (rp_name == "" && rp_oldnametitle != "") rp_name = rp_oldnametitle;
                        if (rp_affil == null && rp_oldorg != null) rp_affil = rp_oldorg;

                        if (rp_name != "" && rp_name != "[Redacted]")
                        {
                            if (sh.CheckPersonName(rp_name))
                            {
                                rp_name = sh.TidyPersonName(rp_name);
                                if (rp_name != "")
                                {
                                    string affil_organisation = null;
                                    if (!sh.AppearsGenuineOrgName(rp_affil))
                                    {
                                        rp_affil = null;
                                    }

                                    if (rp_affil != null)
                                    {
                                        rp_affil = sh.TidyOrgName(rp_affil, sid);
                                        if (!string.IsNullOrEmpty(sponsor_name) 
                                            && rp_affil.ToLower().Contains(sponsor_name.ToLower()))
                                        {
                                            affil_organisation = sponsor_name;
                                        }
                                        else
                                        {
                                            affil_organisation = sh.ExtractOrganisation(rp_affil, sid);
                                        }
                                    }

                                    if (rp_type == "Principal Investigator")
                                    {
                                        contributors.Add(new StudyContributor(sid, 51, "Study Lead",
                                                        rp_name, rp_affil, affil_organisation));
                                    }

                                    if (rp_type == "Sponsor-Investigator")
                                    {
                                        contributors.Add(new StudyContributor(sid, 70, "Sponsor-investigator",
                                                        rp_name, rp_affil, affil_organisation));
                                    }
                                }
                            }
                        }
                    }
                }

                var collaborators = RetrieveListElements(SponsorCollaboratorsModule, "CollaboratorList");
                if (collaborators != null && collaborators.Count() > 0)
                {
                    foreach (XElement Collab in collaborators)
                    {
                        string collab_candidate = FieldValue(Collab, "CollaboratorName");
                        if (sh.AppearsGenuineOrgName(collab_candidate))
                        {
                            string collab_name = sh.TidyOrgName(collab_candidate, sid);
                            contributors.Add(new StudyContributor(sid, 69, "Collaborating organisation", null, collab_name));
                        }
                    }
                }

            }


            if (DescriptionModule != null)
            {
                // CTG descriptions do not seem to include tags, but to be safe....

                s.brief_description = sh.StringClean(FieldValue(DescriptionModule, "BriefSummary"));
            }


            ConditionBrowseModule = RetrieveStruct(DerivedSection, "ConditionBrowseModule");
            if (ConditionBrowseModule != null)
            {
                var condition_meshlist = RetrieveListElements(ConditionBrowseModule, "ConditionMeshList");
                if (condition_meshlist != null && condition_meshlist.Count() > 0)
                {
                    foreach (XElement condition in condition_meshlist)
                    { 
                        string mesh_code = FieldValue(condition, "ConditionMeshId");
                        string mesh_term = FieldValue(condition, "ConditionMeshTerm");
                        topics.Add(new StudyTopic(sid, 13, "condition", true, mesh_code, mesh_term));
                    }
                }
            }

            InterventionBrowseModule = RetrieveStruct(DerivedSection, "InterventionBrowseModule");
            if (InterventionBrowseModule != null)
            {
                var intervention_meshlist = RetrieveListElements(InterventionBrowseModule, "InterventionMeshList");
                if (intervention_meshlist != null && intervention_meshlist.Count() > 0)
                {
                    foreach (XElement intervention in intervention_meshlist)
                    {
                        string mesh_code = FieldValue(intervention, "InterventionMeshId");
                        string mesh_term = FieldValue(intervention, "InterventionMeshTerm");
                        topics.Add(new StudyTopic(sid, 12, "chemical / agent", true, mesh_code, mesh_term));
                    }
                }
            }

            if (ConditionsModule != null)
            {
                var conditions_list = RetrieveListElements(ConditionsModule, "ConditionList");
                if (conditions_list != null && conditions_list.Count() > 0)
                {
                    foreach (XElement condition in conditions_list)
                    {
                        string condition_name = (condition == null) ? null : (string)condition; 

                        // only add the condition name if not already present in the mesh coded conditions
                        if (topic_is_new(condition_name))
                        {
                            topics.Add(new StudyTopic(sid, 13, "condition", condition_name));
                        }
                    }

                }

                var keywords_list = RetrieveListElements(ConditionsModule, "KeywordList");
                if (keywords_list != null && keywords_list.Count() > 0)
                {
                    foreach (XElement keyword in keywords_list)
                    {
                        string keyword_name = (keyword == null) ? null : (string)keyword;

                        // Regularise druig name
                        if (keyword_name.Contains(((char)174).ToString()))
                        {
                            keyword_name = keyword_name.Replace(((char)174).ToString(), "");    // drop reg mark
                            keyword_name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(keyword_name.ToLower());
                        }

                        // only add the condition name if not already present in the mesh coded conditions
                        if (topic_is_new(keyword_name))
                        {
                            topics.Add(new StudyTopic(sid, 11, "keyword", keyword_name));
                        }
                    }
                }
            }


            bool topic_is_new(string candidate_topic)
            {
                foreach (StudyTopic k in topics)
                {
                    if (k.original_value.ToLower() == candidate_topic.ToLower())
                    {
                        return false;
                    }
                }
                return true;
            }


            if (DesignModule != null)
            {
                s.study_type = FieldValue(DesignModule, "StudyType");
                s.study_type_id = th.GetTypeId(s.study_type);

                if (s.study_type == "Interventional")
                {
                    var phases = RetrieveListElements(DesignModule, "PhaseList");
                    if (phases != null && phases.Count() > 0)
                    {
                        foreach (XElement phase in phases)
                        {
                            string this_phase = (phase == null) ? null : (string)phase;
                            features.Add(new StudyFeature(sid, 20, "phase", th.GetPhaseId(this_phase), this_phase));
                        }
                    }
                    else
                    {
                        features.Add(new StudyFeature(sid, 20, "phase", th.GetPhaseId("Not provided"), "Not provided"));
                    }


                    var design_info = RetrieveStruct(DesignModule, "DesignInfo");
                    if (design_info != null)
                    {
                        string design_allocation = FieldValue(design_info, "DesignAllocation") ?? "Not provided";
                        features.Add(new StudyFeature(sid, 22, "allocation type", th.GetAllocationTypeId(design_allocation), design_allocation));

                        string design_intervention_model = FieldValue(design_info, "DesignInterventionModel") ?? "Not provided";
                        features.Add(new StudyFeature(sid, 23, "intervention model", th.GetDesignTypeId(design_intervention_model), design_intervention_model));

                        string design_primary_purpose = FieldValue(design_info, "DesignPrimaryPurpose") ?? "Not provided";
                        features.Add(new StudyFeature(sid, 21, "primary purpose", th.GetPrimaryPurposeId(design_primary_purpose), design_primary_purpose));

                        var masking_details = RetrieveStruct(design_info, "DesignMaskingInfo");
                        if (masking_details != null)
                        {
                            string design_masking = FieldValue(masking_details, "DesignMasking") ?? "Not provided";
                            features.Add(new StudyFeature(sid, 24, "masking", th.GetMaskingTypeId(design_masking), design_masking));
                        }
                        else
                        {
                            features.Add(new StudyFeature(sid, 24, "masking", th.GetMaskingTypeId("Not provided"), "Not provided"));
                        }
                    }
                }


                if (s.study_type == "Observational")
                {
                    string patient_registry = FieldValue(DesignModule, "PatientRegistry");
                    if (patient_registry == "Yes")  // change type...
                    {
                        s.study_type_id = 13;
                        s.study_type = "Observational Patient Registry";
                    }

                    var design_info = RetrieveStruct(DesignModule, "DesignInfo");
                    if (design_info != null)
                    {
                        var obsmodel_list = RetrieveListElements(design_info, "DesignObservationalModelList");
                        if (obsmodel_list != null && obsmodel_list.Count() > 0)
                        {
                                foreach (XElement obsmodel in obsmodel_list)
                                {
                                    string this_obsmodel = (obsmodel == null) ? null : (string)obsmodel;
                                    features.Add(new StudyFeature(sid, 30, "observational model", th.GetObsModelTypeId(this_obsmodel), this_obsmodel));
                                }
                        }
                        else
                        {
                            features.Add(new StudyFeature(sid, 30, "observational model", th.GetObsModelTypeId("Not provided"), "Not provided"));
                        }


                        var timepersp_list = RetrieveListElements(design_info, "DesignTimePerspectiveList");
                        if (timepersp_list != null && timepersp_list.Count() > 0)
                        {
                                foreach (XElement timepersp in timepersp_list)
                                {
                                    string this_persp = (timepersp == null) ? null : (string)timepersp;
                                    features.Add(new StudyFeature(sid, 31, "time perspective", th.GetTimePerspectiveId(this_persp), this_persp));
                                }
                        }
                        else
                        {
                            features.Add(new StudyFeature(sid, 31, "time perspective", th.GetTimePerspectiveId("Not provided"), "Not provided"));
                        }
                    }

                    var biospec_details = RetrieveStruct(DesignModule, "BioSpec");
                    if (biospec_details != null)
                    {
                        string biospec_retention = FieldValue(biospec_details, "BioSpecRetention") ?? "Not provided";
                        features.Add(new StudyFeature(sid, 32, "biospecimens retained", th.GetSpecimentRetentionId(biospec_retention), biospec_retention));
                    }

                }


                var enrol_details = RetrieveStruct(DesignModule, "EnrollmentInfo");
                if (enrol_details != null)
                {
                    string enrolment_count = FieldValue(enrol_details, "EnrollmentCount");
                    if (!string.IsNullOrEmpty(enrolment_count))
                    {
                        // check it is not just a string of 9s

                        if (!Regex.Match(enrolment_count, @"^9+$").Success)
                        {
                            s.study_enrolment = enrolment_count;
                        }
                       
                    }
                }
            }


            if (EligibilityModule != null)
            {
                s.study_gender_elig = FieldValue(EligibilityModule, "Gender") ?? "Not provided";
                if (s.study_gender_elig == "All")
                {
                    s.study_gender_elig = "Both";
                }
                s.study_gender_elig_id = th.GetGenderEligId(s.study_gender_elig);

                string min_age = FieldValue(EligibilityModule, "MinimumAge");
                if (min_age != null)
                {
                    // split number from time unit
                    string LHS = min_age.Trim().Substring(0, min_age.IndexOf(' '));
                    string RHS = min_age.Trim().Substring(min_age.IndexOf(' ') + 1);
                    if (Int32.TryParse(LHS, out int minage))
                    {
                        s.min_age = minage;
                        if (!RHS.EndsWith("s")) RHS += "s";
                        s.min_age_units = RHS;
                        s.min_age_units_id = th.GetTimeUnitsId(RHS);
                    }
                }

                string max_age = FieldValue(EligibilityModule, "MaximumAge");
                if (max_age != null)
                {
                    string LHS = max_age.Trim().Substring(0, max_age.IndexOf(' '));
                    string RHS = max_age.Trim().Substring(max_age.IndexOf(' ') + 1);
                    if (Int32.TryParse(LHS, out int maxage))
                    {
                        s.max_age = maxage;
                        if (!RHS.EndsWith("s")) RHS += "s";
                        s.max_age_units = RHS;
                        s.max_age_units_id = th.GetTimeUnitsId(RHS);
                    }
                }
            }


            if (ContactsLocationsModule != null)
            {
                var officials = RetrieveListElements(ContactsLocationsModule, "OverallOfficialList");

                if (officials != null && officials.Any())
                {
                    foreach (XElement official in officials)
                    {
                        string official_name = FieldValue(official, "OverallOfficialName") ?? "";
                        if (official_name != "" && sh.CheckPersonName(official_name))
                        { 
                            official_name = sh.TidyPersonName(official_name);
                            if (official_name != rp_name)     // check not already present
                            {
                                string official_affiliation = FieldValue(official, "OverallOfficialAffiliation");

                                string affil_organisation = null;
                                if (!sh.AppearsGenuineOrgName(official_affiliation))
                                {
                                    official_affiliation = null;
                                }

                                if (official_affiliation != null)
                                {
                                    official_affiliation = sh.TidyOrgName(official_affiliation, sid);
                                    if (!string.IsNullOrEmpty(sponsor_name)
                                            && official_affiliation.ToLower().Contains(sponsor_name.ToLower()))
                                    {
                                        affil_organisation = sponsor_name;
                                    }
                                    else
                                    {
                                        affil_organisation = sh.ExtractOrganisation(official_affiliation, sid);
                                    }
                                }

                                contributors.Add(new StudyContributor(sid, 51, "Study Lead",
                                                    official_name, official_affiliation, affil_organisation));
                            }
                        }
                    }
                }

                var locations = RetrieveListElements(ContactsLocationsModule, "LocationList");

                if (locations != null && locations.Any())
                {
                    foreach (XElement location in locations)
                    {
                        string facility = null;
                        string fac = sh.ReplaceApos(sh.TrimString(FieldValue(location, "LocationFacility")));
                        if (fac != null)
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
                                facility = facility.Replace("Ctr", "Center");  // N.B. US spelling
                            }
                            if (facility.EndsWith(" Hosp") || facility.Contains("Hosp "))
                            {
                                facility = facility.Replace("Hosp", "Hospital");  // N.B. US spelling
                            }
                        }
                        string city = FieldValue(location, "LocationCity");
                        string country = FieldValue(location, "LocationCountry");
                        string status = FieldValue(location, "LocationStatus");
                        int? status_id = string.IsNullOrEmpty(status) ? null : th.GetStatusId(status);
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
                            st.country_name = sh.ReplaceApos(st.country_name.Trim());
                            if (countries.Count == 0)
                            {
                                countries.Add(new StudyCountry(st.sd_sid, st.country_name));
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
                                    countries.Add(new StudyCountry(st.sd_sid, st.country_name));
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
                string IPDSharing = FieldValue(IPDSharingModule, "IPDSharing");

                if (IPDSharing != null)
                {
                    sharing_statement = "IPD Sharing: " + sh.StringClean(IPDSharing) + " (as of " + status_verified_date + ")";
                }
                    
                string IPDSharingDescription = FieldValue(IPDSharingModule, "IPDSharingDescription");

                if (IPDSharingDescription != null)
                {
                    sharing_statement += "\nDescription: " + sh.StringClean(IPDSharingDescription);

                    string IPDSharingTimeFrame = FieldValue(IPDSharingModule, "IPDSharingTimeFrame") ?? "";
                    if (IPDSharingTimeFrame != "")
                    {
                        sharing_statement += "\nTime frame: " + sh.StringClean(IPDSharingTimeFrame);
                    }

                    string IPDSharingAccessCriteria = FieldValue(IPDSharingModule, "IPDSharingAccessCriteria") ?? "";
                    if (IPDSharingAccessCriteria != "")
                    {
                        sharing_statement += "\nAccess Criteria: " + sh.StringClean(IPDSharingAccessCriteria);
                    }

                    string IPDSharingURL = FieldValue(IPDSharingModule, "IPDSharingURL") ?? "";
                    if (IPDSharingURL != "")
                    {
                        sharing_statement += "\nURL: " + sh.StringClean(IPDSharingURL);
                    }

                    var IPDSharingInfoTypeList = RetrieveListElements(IPDSharingModule, "IPDSharingInfoTypeList");
                    if (IPDSharingInfoTypeList != null && IPDSharingInfoTypeList.Count() > 0)
                    {
                        string itemlist = "";
                        foreach (XElement infotype in IPDSharingInfoTypeList)
                        {
                            string item_type = (infotype == null) ? null : (string)infotype;
                            itemlist += (item_type != null) ? ", " + item_type : "";
                        }

                        string infoitemlist = sh.StringClean(itemlist.Substring(1));
                        sharing_statement += "\nAdditional information available: " + infoitemlist;
                    }
                   
                }

                s.data_sharing_statement = sharing_statement;
            }


            #region Establish Linked Data Objects

            /********************* Linked Data Object Data **********************************/

            string object_type = "", object_class = "";
            string title_base = "";
            int title_type_id = 0;
            string title_type = "";
            string url = "";

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


            // set up initial registry entry data objects 
            // establish base for title
            if (brief_title != null)
            {
                title_base = brief_title;
                title_type_id = 22;
                title_type = "Study short name :: object type";
            }
            else if (official_title != null)
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

            
            // first object is the protocol registration
            // title will be display title as well
            string object_title = "CTG registry entry";
            string object_display_title = title_base + " :: CTG registry entry";

            // create hash Id for the data object
            string sd_oid = sid + " :: 13 :: " + object_title;

            // Define and provide intiial values
            int object_type_id = 13;
            int object_class_id = 23;

            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, firstpost.year,
                                23, "Text", 13, "Trial Registry entry", 100120,
                                "ClinicalTrials.gov", 12, download_datetime));

            // add in title
            object_titles.Add(new ObjectTitle(sd_oid, object_display_title, title_type_id, title_type, true));

            // add in dates
            if (firstpost != null)
            {
                object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                                        firstpost.year, firstpost.month, firstpost.day, firstpost.date_string));
            }
            if (updatepost != null)
            {
                object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                                        updatepost.year, updatepost.month, updatepost.day, updatepost.date_string));
            }

            // add in instance
            url = "https://clinicaltrials.gov/ct2/show/study/" + sid;
            object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true,
                                      39, "Web text with XML or JSON via API"));
                        
            // if present, set up results data object
            if (resultspost != null && results_data_present)
            {
                object_title = "CTG results entry";
                object_display_title = title_base + " :: CTG results entry";
                sd_oid = sid + " :: 28 :: " + object_title;

                data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, resultspost.year,
                                    23, "Text", 28, "Trial registry results summary", 100120,
                                    "ClinicalTrials.gov", 12, download_datetime));

                // add in title
                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                title_type_id, title_type, true));

                // add in dates
                if (resultspost != null)
                {
                    object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                                            resultspost.year, resultspost.month, resultspost.day, resultspost.date_string));
                }
                if (updatepost != null)
                {
                    object_dates.Add(new ObjectDate(sd_oid, 18, "Updated",
                                            updatepost.year, updatepost.month, updatepost.day, updatepost.date_string));
                }

                // add in instance
                url = "https://clinicaltrials.gov/ct2/show/results/" + sid;
                object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true,
                                                   39, "Web text with XML or JSON via API"));

            }


            if (LargeDocumentModule != null)
            {
                if (LargeDocumentModule != null)
                {
                    var largedocs = RetrieveListElements(LargeDocumentModule, "LargeDocList");
                    if (largedocs != null && largedocs.Count() > 0)
                    {
                        foreach (XElement largedoc in largedocs)
                        {
                            string type_abbrev = FieldValue(largedoc, "LargeDocTypeAbbrev");
                            string has_protocol = FieldValue(largedoc, "LargeDocHasProtocol");
                            string has_sap = FieldValue(largedoc, "LargeDocHasSAP");
                            string has_icf = FieldValue(largedoc, "LargeDocHasICF");
                            string doc_label = FieldValue(largedoc, "LargeDocLabel");
                            string doc_date = FieldValue(largedoc, "LargeDocDate");
                            string upload_date = FieldValue(largedoc, "LargeDocUploadDate");
                            string file_name = FieldValue(largedoc, "LargeDocFilename");

                            // create a new data object

                            // decompose the doc date to get creation year
                            // and upload date to get publication year
                            SplitDate docdate = null;
                            if (doc_date != null)
                            {
                                docdate = dh.GetDatePartsFromCTGString(doc_date);
                            }
                            SplitDate uploaddate = null;
                            if (upload_date != null)
                            {
                                // machine generated - uses mm/dd/yyyy time format
                                uploaddate = dh.GetDatePartsFromUSString(upload_date.Substring(0, 10));
                            }

                            switch (type_abbrev)
                            {
                                case "Prot":
                                    {
                                        object_type_id = 11; object_type = "Study protocol";
                                        break;
                                    }
                                case "SAP":
                                    {
                                        object_type_id = 22; object_type = "Statistical analysis plan";
                                        break;
                                    }
                                case "ICF":
                                    {
                                        object_type_id = 18; object_type = "Informed consent forms";
                                        break;
                                    }
                                case "Prot_SAP":
                                    {
                                        object_type_id = 74; object_type = "Protocol SAP";
                                        break;
                                    }
                                case "Prot_ICF":
                                    {
                                        object_type_id = 75; object_type = "Protocol ICF";
                                        break;
                                    }
                                case "Prot_SAP_ICF":
                                    {
                                        object_type_id = 76; object_type = "Protocol SAP ICF";
                                        break;
                                    }
                                default:
                                    {
                                        object_type_id = 37; object_type = type_abbrev;
                                        break;
                                    }
                            }

                            int t_type_id; string t_type;

                            // title type depends on whether label is present

                            if (!string.IsNullOrEmpty(doc_label))
                            {
                                object_display_title = title_base + " :: " + doc_label;
                                t_type_id = 21; t_type = "Study short name :: object name";
                            }
                            else
                            {
                                object_display_title = title_base + " :: " + object_type;
                                t_type_id = 22; t_type = "Study short name :: object type";
                            }

                            // check name
                            int next_num = CheckObjectName(object_titles, object_display_title);
                            if (next_num > 0)
                            {
                                object_display_title += "_" + next_num.ToString();
                            }
                            object_title = object_display_title.Substring(title_base.Length + 4);
                            sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;

                            data_objects.Add(new DataObject(sd_oid, sid, object_title, object_display_title, uploaddate?.year,
                            23, "Text", object_type_id, object_type, 100120,
                            "ClinicalTrials.gov", 11, download_datetime));

                            // check here not a previous data object of the same type
                            // It may have the same url. If so ignore it.
                            // If it appears to be different, add a suffix to the data object name

                            // add in title
                            object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                            t_type_id, t_type, true));

                            // add in dates
                            if (docdate != null)
                            {
                                object_dates.Add(new ObjectDate(sd_oid, 15, "Created",
                                    docdate.year, docdate.month, docdate.day, docdate.date_string));
                            }

                            if (upload_date != null)
                            {
                                object_dates.Add(new ObjectDate(sd_oid, 12, "Available",
                                uploaddate.year, uploaddate.month, uploaddate.day, uploaddate.date_string));
                            }

                            // add in instance
                            url = "https://clinicaltrials.gov/ProvidedDocs/" + sid.Substring(sid.Length - 2, 2) + "/" + sid + "/" + file_name;
                            object_instances.Add(new ObjectInstance(sd_oid, 100120, "ClinicalTrials.gov", url, true, 11, "PDF"));
                        }
                    }
                }
            }


            if (ReferencesModule != null)
            {
                // references cannot become data objects until
                // their dates are checked against the study date
                // this is therefore generating a list for the future.

                var refs = RetrieveListElements(ReferencesModule, "ReferenceList");
                if (refs != null && refs.Count() > 0)
                {
                    foreach (XElement reference in refs)
                    {
                        string ref_type = FieldValue(reference, "ReferenceType");
                        if (ref_type == "result" || ref_type == "derived")
                        {
                            string pmid = FieldValue(reference, "ReferencePMID");
                            string citation = sh.ReplaceApos(FieldValue(reference, "ReferenceCitation"));
                            references.Add(new StudyReference(sid, pmid, citation, null, null));
                        }

                        var retractions = RetrieveListElements(reference, "RetractionList");
                        if (retractions != null && retractions.Count() > 0)
                        {
                            foreach (XElement retraction in retractions)
                            {
                                string retraction_pmid = FieldValue(retraction, "RetractionPMID");
                                string retraction_source = FieldValue(retraction, "RetractionSource");
                                references.Add(new StudyReference(sid, retraction_pmid, retraction_source, null, "RETRACTION"));
                            }
                        }
                    }
                }


                // some of the available ipd may be turnable into data objects available, either
                // directly or after review of requests
                // Others will need to be stored as records for future processing

                var avail_ipd_items = RetrieveListElements(ReferencesModule, "AvailIPDList");
                if (avail_ipd_items != null && avail_ipd_items.Count() > 0)
                {
                    foreach (XElement avail_ipd in avail_ipd_items)
                    {
                        string ipd_id = FieldValue(avail_ipd, "AvailIPDId") ?? "";
                        string ipd_type = FieldValue(avail_ipd, "AvailIPDType") ?? "";
                        string ipd_url = FieldValue(avail_ipd, "AvailIPDURL") ?? "";
                        string ipd_comment = FieldValue(avail_ipd, "AvailIPDComment") ?? "";
                        ipd_comment = sh.ReplaceApos(ipd_comment);

                        // Often a GSK store

                        if (ipd_url.Contains("clinicalstudydatarequest.com"))
                        {   
                            object_type_id = 0;

                            // create a new data object
                            switch (ipd_type)
                            {
                                case "Informed Consent Form":
                                    {
                                        object_type_id = 18; object_type = "Informed consent forms";
                                        break;
                                    }
                                case "Dataset Specification":
                                    {
                                        object_type_id = 31; object_type = "Data dictionary";
                                        break;
                                    }
                                case "Annotated Case Report Form":
                                    {
                                        object_type_id = 30; object_type = "Annotated data collection forms";
                                        break;
                                    }
                                case "Statistical Analysis Plan":
                                    {
                                        object_type_id = 22; object_type = "Statistical analysis plan";
                                        break;
                                    }
                                case "Individual Participant Data Set":
                                    {
                                        object_type_id = 80; object_type = "Individual participant data";
                                        break;
                                    }
                                case "Clinical Study Report":
                                    {
                                        object_type_id = 26; object_type = "Clinical study report";
                                        break;
                                    }
                                case "Study Protocol":
                                    {
                                        object_type_id = 11; object_type = "Study protocol";
                                        break;
                                    }
                            }

                            if (object_type_id != 0)
                            {
                                object_class_id = (object_type_id == 80) ? 14 : 23;
                                object_class = (object_type_id == 80) ? "Dataset" : "Text";

                                int? sponsor_id = null;
                                string t_base = "";

                                if (sponsor_name == "GlaxoSmithKline" || sponsor_name == "GSK")
                                {
                                    sponsor_id = 100163;
                                    t_base = "GSK-";
                                }
                                else
                                {
                                    sponsor_id = null;
                                    t_base = sponsor_name + "-" ?? "";
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
                                    if (sponsor_name == "GlaxoSmithKline" || sponsor_name == "GSK")
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

                        else if (ipd_url.Contains("servier.com"))
                        {
                            // create a new data object

                            object_type_id = 0;
                           
                            if (ipd_type.ToLower().Contains("study-level clinical trial data"))
                            {
                                object_type_id = 69; object_type = "Aggregated result dataset";
                            }
                            else
                            {
                                switch (ipd_type)
                                {
                                    case "Informed Consent Form":
                                        {
                                            object_type_id = 18; object_type = "Informed consent forms";
                                            break;
                                        }
                                    case "Statistical Analysis Plan":
                                        {
                                            object_type_id = 22; object_type = "Statistical analysis plan";
                                            break;
                                        }
                                    case "Individual Participant Data Set":
                                        {
                                            object_type_id = 80; object_type = "Individual participant data";
                                            break;
                                        }
                                    case "Clinical Study Report":
                                        {
                                            object_type_id = 26; object_type = "Clinical study report";
                                            break;
                                        }
                                    case "Study Protocol":
                                        {
                                            object_type_id = 11; object_type = "Study protocol";
                                            break;
                                        }
                                }
                            }

                            if (object_type_id != 0)
                            {
                                object_class_id = (object_type_id == 80 || object_type_id == 69) ? 14 : 23;
                                object_class = (object_type_id == 80 || object_type_id == 69) ? "Dataset" : "Text";

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
                                                3, "Anonymised", "Sevier states that... 'Servier will provide anonymized patient-level and study-level clinical trial data'",
                                                2, "De-identification applied", null,
                                                0, "Not known", null));
                                }
                            }
                            else
                            {
                                // store data for later inspection
                                ipd_info.Add(new AvailableIPD(sid, ipd_id, ipd_type, ipd_url, ipd_comment));
                            }
                        }

                        else if (ipd_url.Contains("merck.com"))
                        {

                            // some of the merck records are direct access to a page
                            // with a further link to a pdf, plus other study components

                            // the others are indications that the object exists but is not directly available
                            // create a new data object

                            if (ipd_url.Contains("&tab=access"))
                            {
                                object_type_id = 79; object_type = "CSR summary";
                                object_class_id = 23; object_class = "Text";

                                // disregard the other entries - as they lead nowhere
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
                                object_class_id, object_class, object_type_id, object_type,
                                100165, "Merck Sharp & Dohme", 11, download_datetime));

                                // add in title
                                title_type_id = 22; title_type = "Study short name :: object type";
                                object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                title_type_id, title_type, true));

                                // add in instance
                                object_instances.Add(new ObjectInstance(sd_oid, 4, "Summary version", 100165,
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

                            object_type_id = 0;

                            string ipd_name = ipd_type;
                            string ipd_type_lower = ipd_type.ToLower();
                            if (ipd_type_lower.StartsWith("study") && 
                                 (  ipd_type_lower.Contains("design") || ipd_type_lower.Contains("details")
                                 || ipd_type_lower.Contains("overview") || ipd_type_lower.Contains("summary")
                                 || ipd_type_lower.Contains("synopsis")))
                            {
                                ipd_type_lower = "study summary";
                            }
                            if (ipd_type_lower.StartsWith("complete set of descriptive data"))
                            {
                                ipd_type_lower = "study summary";
                            }

                            // used as defaults if not over-written by best guesses
                            int resource_type_id = 0;
                            string resource_type = "Not yet known";

                            switch (ipd_type_lower)
                            {
                                case "study protocol":
                                    {
                                        object_type_id = 11; object_type = "Study protocol";
                                        resource_type_id = 11; resource_type = "PDF";
                                        break;
                                    }
                                case "individual participant data set":
                                    {
                                        object_type_id = 80; object_type = "Individual participant data";
                                        break;
                                    }  
                                case "clinical study report":
                                    {
                                        object_type_id = 26; object_type = "Clinical study report";
                                        resource_type_id = 11; resource_type = "PDF";
                                        break;
                                    }
                                case "informed consent form":
                                    {
                                        object_type_id = 18; object_type = "Informed consent forms";
                                        resource_type_id = 11; resource_type = "PDF";
                                        break;
                                    }
                                case "study forms":
                                    {
                                        object_type_id = 21; object_type = "Data collection forms";
                                        break;
                                    }
                                case "statistical analysis plan":
                                    {
                                        object_type_id = 22; object_type = "Statistical analysis plan";
                                        resource_type_id = 11; resource_type = "PDF";
                                        break;
                                    }
                                case "manual of procedure":
                                    {
                                        object_type_id = 36; object_type = "Manual of procedures";
                                        resource_type_id = 11; resource_type = "PDF";
                                        break;
                                    }
                                case "analytic code":
                                    {
                                        object_type_id = 29; object_type = "Analysis notes";
                                        break;
                                    }
                                case "study summary":
                                    {
                                        object_type_id = 38; object_type = "Study overview";
                                        break;
                                    }
                                case "data coding manuals":
                                    {
                                        object_type_id = 82; object_type = "Data coding manual";
                                        resource_type_id = 11; resource_type = "PDF";
                                        break;
                                    }
                                case "questionnaire":
                                    {
                                        object_type_id = 40; object_type = "Standard instruments";

                                        break;
                                    }
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

                                string repo_org_name = null;
                                if (ipd_url.Contains("immport")) repo_org_name = "Immport";
                                if (ipd_url.Contains("itntrialshare")) repo_org_name = "Immune Tolerance Network";
                                if (ipd_url.Contains("drive.google")) repo_org_name = "Google Drive";
                                if (ipd_url.Contains("dataverse")) repo_org_name = "Dataverse";
                                if (ipd_url.Contains("datadryad")) repo_org_name = "Datadryad";
                                if (ipd_url.Contains("github")) repo_org_name = "GitHub"; 
                                if (ipd_url.Contains("osf.io")) repo_org_name = "Open Science Foundation";
                                if (ipd_url.Contains("scribd")) repo_org_name = "Scribd";
                                if (ipd_url.Contains("researchgate")) repo_org_name = "Research Gate";
                                if (ipd_url.Contains("zenodo")) repo_org_name = "Zenodo";

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

                // at the moment these records are for storage and future processing
                // tidy up urls, remove a small proportion of obvious non-useful links

                var see_also_refs = RetrieveListElements(ReferencesModule, "SeeAlsoLinkList");
                if (see_also_refs != null && see_also_refs.Count() > 0)
                {
                    foreach (XElement see_also_ref in see_also_refs)
                    {
                        string link_label = FieldValue(see_also_ref, "SeeAlsoLinkLabel") ?? "";
                        link_label = link_label.Trim(' ', '|', '.', ':', '\"');
                        if (link_label.StartsWith('(') && link_label.EndsWith(')'))
                        {
                            link_label = link_label.Trim('(', ')');
                        }
                        link_label = sh.ReplaceApos(link_label);

                        string link_url = FieldValue(see_also_ref, "SeeAlsoLinkURL") ?? "";
                        link_url = link_url.Trim(' ', '/');

                        if (link_url != "")
                        {
                            bool add_to_db = true;
                            if (link_label == "NIH Clinical Center Detailed Web Page" && link_url.EndsWith(".html"))
                            {
                                // add new data object
                                object_type_id = 38; object_type = "Study Overview";
                                object_class_id = 23; object_class = "Text";

                                // disregard the other entries - as they lead nowhere
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

                                add_to_db = false;
                            }

                            if (link_url.Contains("filehosting.pharmacm.com/Download"))
                            {
                                string test_url = link_url.ToLower();
                                object_type_id = 0;
                                int instance_type_id = 1; // default
                                string instance_type = "Full resource"; // default

                                if (test_url.Contains("csr") || (test_url.Contains("study") && test_url.Contains("report")))
                                {
                                    if (test_url.Contains("redacted"))
                                    {
                                        object_type_id = 27; object_type = "Redacted Clinical Study Report";
                                        instance_type_id = 5; instance_type = "Redacted version";
                                    }
                                    else if (test_url.Contains("summary"))
                                    {
                                        object_type_id = 79; object_type = "CSR Summary";
                                        instance_type_id = 4; instance_type = "Summary version";
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
                                        instance_type_id = 5; instance_type = "Redacted version";
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
                                        instance_type_id = 5; instance_type = "Redacted version";
                                    }
                                    else
                                    {
                                        object_type_id = 22; object_type = "Statistical analysis plan";
                                    }
                                }

                                else if (test_url.Contains("summary") || test_url.Contains("rds"))
                                {
                                    object_type_id = 79; object_type = "CSR summary";
                                    instance_type_id = 4; instance_type = "Summary version";
                                }

                                else if (test_url.Contains("poster"))
                                {
                                    object_type_id = 108; object_type = "Conference Poster";
                                }


                                if (object_type_id > 0 && sponsor_name != null)
                                {
                                    // Probably need to add a new data object. By default....

                                    object_display_title = title_base + " :: " + object_type;

                                    // check name
                                    int next_num = CheckObjectName(object_titles, object_display_title);
                                    if (next_num > 0)
                                    {
                                        object_display_title += "_" + next_num.ToString();
                                    }
                                    object_title = object_display_title.Substring(title_base.Length + 4);

                                    sd_oid = sid + " :: " + object_type_id.ToString() + " :: " + object_title;
                                    // check here not a previous data object of the same type
                                    // It may have the same url. If so ignore it.
                                    // If it appears to be different, add a suffix to the data object name

                                    object_class_id = 23; object_class = "Text";
                                    DataObject doc_object = new DataObject(sd_oid, sid, object_title, object_display_title, null,
                                    23, "Text", object_type_id, object_type, null, sponsor_name, 11, download_datetime);

                                    // add data object
                                    data_objects.Add(doc_object);

                                    // add in title
                                    title_type_id = 22; title_type = "Study short name :: object type";
                                    object_titles.Add(new ObjectTitle(sd_oid, object_display_title,
                                    title_type_id, title_type, true));

                                    // add in instance
                                    object_instances.Add(new ObjectInstance(sd_oid, instance_type_id, instance_type,
                                        101419, "TrialScope Disclose", link_url, true, 11, "PDF", null, null));

                                }
                            }

                            if (link_label == "To obtain contact information for a study center near you, click here.") add_to_db = false;
                            if (link_label == "Researchers can use this site to request access to anonymised patient level data and/or supporting documents from clinical studies to conduct further research.") add_to_db = false;
                            if (link_label == "University of Texas MD Anderson Cancer Center Website") add_to_db = false;
                            if (link_label == "UT MD Anderson Cancer Center website") add_to_db = false;
                            if (link_label == "Clinical Trials at Novo Nordisk") add_to_db = false;
                            if (link_label == "Memorial Sloan Kettering Cancer Center") add_to_db = false;
                            if (link_label == "AmgenTrials clinical trials website") add_to_db = false;
                            if (link_label == "Mayo Clinic Clinical Trials") add_to_db = false;
                            if (link_url == "http://trials.boehringer-ingelheim.com") add_to_db = false;
                            if ((link_label == null || link_label == "") && (link_url.EndsWith(".com") || link_url.EndsWith(".org"))) add_to_db = false;

                            // only add to links table if all tests above have failed

                            if (add_to_db && link_label != "")
                            {
                                studylinks.Add(new StudyLink(sid, link_label, link_url));
                            }
                        }
                    }
                }
            }

            #endregion

            // edit contributors - try to ensure properly categorised
            if (contributors.Count > 0)
            {
                foreach (StudyContributor sc in contributors)
                {
                    if (!sc.is_individual)
                    {
                        // identify individuals down as organisations

                        string orgname = sc.organisation_name.ToLower();
                        if (ih.CheckIfIndividual(orgname))
                        {
                            sc.person_full_name = sh.TidyPersonName(sc.organisation_name);
                            sc.organisation_name = null;
                            sc.is_individual = true;

                            // Change to a sponsor investigator (was a sponsor)
                            sc.contrib_type_id = 70;
                            sc.contrib_type = "Sponsor-investigator"; 
                        }
                        else if (orgname == "sponsor" || orgname == "company internal")
                        {
                            // seems to be unique to Clinical Trials.gov
                            sc.organisation_name = sponsor_name;
                        }
                    }
                    else
                    {
                        // check if a group inserted as an individual

                        string fullname = sc.person_full_name.ToLower();
                        if (ih.CheckIfOrganisation(fullname))
                        {
                            sc.organisation_name = sh.TidyOrgName(sid, sc.person_full_name);
                            sc.person_full_name = null;
                            sc.is_individual = false;
                        }
                    }
                }

                // try to identify repeated individuals...
                // can happen as paeople are put in under different categories

                int n = 0;
                foreach (StudyContributor sc in contributors)
                {
                    bool add_sc = true;
                    if (sc.is_individual)
                    {
                        n++;
                        if (n > 1)
                        {
                            foreach (StudyContributor sc2 in contributors2)
                            {
                                if (sc.person_full_name == sc2.person_full_name)
                                {
                                    add_sc = false;

                                    // but retain this info if needed / possible

                                    if (string.IsNullOrEmpty(sc2.person_affiliation) 
                                        && !string.IsNullOrEmpty(sc.person_affiliation))
                                    {
                                        sc2.person_affiliation = sc.person_affiliation;
                                    }
                                    if (string.IsNullOrEmpty(sc2.organisation_name)
                                        && !string.IsNullOrEmpty(sc.organisation_name))
                                    {
                                        sc2.organisation_name = sc.organisation_name;
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    if(add_sc)
                    {
                        contributors2.Add(sc);
                    }
                }
            }


            s.identifiers = identifiers;
            s.titles = titles;
            s.contributors = contributors2;
            s.references = references;
            s.studylinks = studylinks;
            s.ipd_info = ipd_info;
            s.topics = topics;
            s.features = features;
            s.relationships = relationships;
            s.sites = sites;  
            s.countries = countries;

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
                for (int j = 0; j < titles.Count; j++)
                {
                    if (titles[j].title_text.Contains(object_display_title))
                    {
                        num_of_this_type++;
                    }
                }
            }
            return num_of_this_type;
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


        private XElement RetrieveStruct(XElement container, string nameToMatch)
        {
            var Structs = container.Elements("Struct");
            foreach (XElement st in Structs)
            {
                if ((string)st.Attribute("Name") == nameToMatch)
                {
                    return st;
                }
            }
            return null;
        }


        private bool CheckStructExists(XElement container, string nameToMatch)
        {

            var Structs = container.Elements("Struct");
            foreach (XElement st in Structs)
            {
                if ((string)st.Attribute("Name") == nameToMatch)
                {
                    return true;
                }
            }
            return false;
        }


        private string FieldValue(XElement container, string nameToMatch)
        {
            var Fields = container.Elements("Field");
            foreach (XElement b in Fields)
            {
                if ((string)b.Attribute("Name") == nameToMatch)
                {
                    return (b == null) ? null : (string)b;
                }
            }
            return null;
        }


        private string StructFieldValue(XElement container, string structToMatch, string fieldToMatch)
        {
            var Structs = container.Elements("Struct");
            foreach (XElement st in Structs)
            {
                if ((string)st.Attribute("Name") == structToMatch)
                {
                    return FieldValue(st, fieldToMatch);
                }
            }
            return null;
        }


        private IEnumerable<XElement> RetrieveListElements(XElement container, string listToMatch)
        {
            var Lists = container.Elements("List");
            foreach (XElement li in Lists)
            {
                if ((string)li.Attribute("Name") == listToMatch)
                {
                    return li.Elements();
                }
            }
            return null;
        }

    }
}