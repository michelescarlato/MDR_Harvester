
namespace MDR_Harvester.Ctg;

public class CTG_Record
{
    public Protocolsection? ProtocolSection { get; set; }
    public Derivedsection? DerivedSection { get; set; }
    public Documentsection? DocumentSection { get; set; }
}

public class Protocolsection
{
    public Identificationmodule? IdentificationModule { get; set; }
    public Statusmodule? StatusModule { get; set; }
    public Sponsorcollaboratorsmodule? SponsorCollaboratorsModule { get; set; }
    public Oversightmodule? OversightModule { get; set; }
    public Descriptionmodule? DescriptionModule { get; set; }
    public Conditionsmodule? ConditionsModule { get; set; }
    public Designmodule? DesignModule { get; set; }
    public Eligibilitymodule? EligibilityModule { get; set; }
    public Contactslocationsmodule? ContactsLocationsModule { get; set; }
    public Referencesmodule? ReferencesModule { get; set; }
    public Ipdsharingstatementmodule? IPDSharingStatementModule { get; set; }
}


public class Identificationmodule
{
    public string? NCTId { get; set; }
    public NctIdaliaslist? NCTIdAliasList { get; set; }
    public Orgstudyidinfo? OrgStudyIdInfo { get; set; }
    public Organization? Organization { get; set; }
    public string? BriefTitle { get; set; }
    public string? OfficialTitle { get; set; }
    public string? Acronym { get; set; }
    public Secondaryidinfolist? SecondaryIdInfoList { get; set; }
}

public class NctIdaliaslist
{
    public string[]? NCTIdAlias { get; set; }
}

public class Orgstudyidinfo
{
    public string? OrgStudyId { get; set; }
    public string? OrgStudyIdType { get; set; }
    public string? OrgStudyIdDomain { get; set; }
    public string? OrgStudyIdLink { get; set; }
}

public class Organization
{
    public string? OrgFullName { get; set; }
    public string? OrgClass { get; set; }
}

public class Secondaryidinfolist
{
    public Secondaryidinfo[]? SecondaryIdInfo { get; set; }
}

public class Secondaryidinfo
{
    public string? SecondaryId { get; set; }
    public string? SecondaryIdType { get; set; }
    public string? SecondaryIdDomain { get; set; }
    public string? SecondaryIdLink { get; set; }
}



public class Statusmodule
{
    public string? StatusVerifiedDate { get; set; }
    public string? OverallStatus { get; set; }
    public string? LastKnownStatus { get; set; }
    public Expandedaccessinfo? ExpandedAccessInfo { get; set; }
    public Startdatestruct? StartDateStruct { get; set; }
    public Primarycompletiondatestruct? PrimaryCompletionDateStruct { get; set; }
    public Completiondatestruct? CompletionDateStruct { get; set; }
    public string? StudyFirstSubmitDate { get; set; }
    public string? StudyFirstSubmitQCDate { get; set; }
    public Studyfirstpostdatestruct? StudyFirstPostDateStruct { get; set; }
    public string? ResultsFirstSubmitDate { get; set; }
    public Resultsfirstpostdatestruct? ResultsFirstPostDateStruct { get; set; }
    public string? LastUpdateSubmitDate { get; set; }
    public Lastupdatepostdatestruct? LastUpdatePostDateStruct { get; set; }
}

public class Expandedaccessinfo
{
    public string? HasExpandedAccess { get; set; }
    public string? ExpandedAccessNCTId { get; set; }
    public string? ExpandedAccessStatusForNCTId { get; set; }
}

public class Startdatestruct
{
    public string? StartDate { get; set; }
    public string? StartDateType { get; set; }
}

public class Primarycompletiondatestruct
{
    public string? PrimaryCompletionDate { get; set; }
    public string? PrimaryCompletionDateType { get; set; }
}

public class Completiondatestruct
{
    public string? CompletionDate { get; set; }
    public string? CompletionDateType { get; set; }
}

public class Resultsfirstpostdatestruct
{
    public string? ResultsFirstPostDate { get; set; }
    public string? ResultsFirstPostDateType { get; set; }
}

public class Studyfirstpostdatestruct
{
    public string? StudyFirstPostDate { get; set; }
    public string? StudyFirstPostDateType { get; set; }
}

public class Lastupdatepostdatestruct
{
    public string? LastUpdatePostDate { get; set; }
    public string? LastUpdatePostDateType { get; set; }
}

public class Sponsorcollaboratorsmodule
{
    public Responsibleparty? ResponsibleParty { get; set; }
    public Leadsponsor? LeadSponsor { get; set; }
    public Collaboratorlist? CollaboratorList { get; set; }
}


public class Responsibleparty
{
    public string? ResponsiblePartyType { get; set; }
    public string? ResponsiblePartyInvestigatorFullName { get; set; }
    public string? ResponsiblePartyInvestigatorTitle { get; set; }
    public string? ResponsiblePartyInvestigatorAffiliation { get; set; }
    public string? ResponsiblePartyOldNameTitle { get; set; }
    public string? ResponsiblePartyOldOrganization { get; set; }
}

public class Leadsponsor
{
    public string? LeadSponsorName { get; set; }
    public string? LeadSponsorClass { get; set; }
}

public class Collaboratorlist
{
    // Need to change property name to Collaborators

    public Collaborator[]? Collaborator { get; set; }
}

public class Collaborator
{
    public string? CollaboratorName { get; set; }
    public string? CollaboratorClass { get; set; }
}


public class Oversightmodule
{
    public string? OversightHasDMC { get; set; }
    public string? IsFDARegulatedDrug { get; set; }
    public string? IsFDARegulatedDevice { get; set; }
    public string? IsUnapprovedDevice { get; set; }
    public string? IsPPSD { get; set; }
    public string? IsUSExport { get; set; }
    public string? FDAAA801Violation { get; set; }
}

public class Descriptionmodule
{
    public string? BriefSummary { get; set; }
    public string? DetailedDescription { get; set; }
}

public class Conditionsmodule
{
    public Conditionlist? ConditionList { get; set; }
    public Keywordlist? KeywordList { get; set; }
}

public class Conditionlist
{
    public string[]? Condition { get; set; }
}

public class Keywordlist
{
    public string[]? Keyword { get; set; }
}

public class Designmodule
{
    public string? StudyType { get; set; }
    public string? PatientRegistry { get; set; }
    public string? TargetDuration { get; set; }
    public Designinfo? DesignInfo { get; set; }
    public Enrollmentinfo? EnrollmentInfo { get; set; }
    public Phaselist? PhaseList { get; set; }
    public Biospec? BioSpec { get; set; }
}

public class Designinfo
{
    public Designobservationalmodellist? DesignObservationalModelList { get; set; }
    public Designtimeperspectivelist? DesignTimePerspectiveList { get; set; }
    public string? DesignAllocation { get; set; }
    public string? DesignInterventionModel { get; set; }
    public string? DesignInterventionModelDescription { get; set; }
    public string? DesignPrimaryPurpose { get; set; }
    public Designmaskinginfo? DesignMaskingInfo { get; set; }
}

public class Designobservationalmodellist
{
    public string[]? DesignObservationalModel { get; set; }
}

public class Designtimeperspectivelist
{
    public string[]? DesignTimePerspective { get; set; }
}

public class Designmaskinginfo
{
    public string? DesignMasking { get; set; }
    public string? DesignMaskingDescription { get; set; }
    public Designwhomaskedlist? DesignWhoMaskedList { get; set; }
}

public class Designwhomaskedlist
{
    public string[]? DesignWhoMasked { get; set; }
}

public class Enrollmentinfo
{
    public string? EnrollmentCount { get; set; }
    public string? EnrollmentType { get; set; }
}

public class Phaselist
{
    public string[]? Phase { get; set; }
}

public class Biospec
{
    public string? BioSpecRetention { get; set; }
    public string? BioSpecDescription { get; set; }
}


public class Interventionlist
{
    public Intervention[]?Intervention { get; set; }
}

public class Intervention
{
    public string? InterventionType { get; set; }
    public string? InterventionName { get; set; }
    public string? InterventionDescription { get; set; }
    public Interventionarmgrouplabellist? InterventionArmGroupLabelList { get; set; }
    public Interventionothernamelist? InterventionOtherNameList { get; set; }
}
 
public class Interventionarmgrouplabellist
{
    public string[]? InterventionArmGroupLabel { get; set; }
}

public class Interventionothernamelist
{
    public string[]? InterventionOtherName { get; set; }
}


public class Eligibilitymodule
{
    public string? EligibilityCriteria { get; set; }
    public string? HealthyVolunteers { get; set; }
    public string? Gender { get; set; }
    public string? GenderBased { get; set; }
    public string? GenderDescription { get; set; }
    public string? MinimumAge { get; set; }
    public string? MaximumAge { get; set; }
    public Stdagelist? StdAgeList { get; set; }
    public string? StudyPopulation { get; set; }
    public string? SamplingMethod { get; set; }
}

public class Stdagelist
{
    public string[]? StdAge { get; set; }
}

public class Contactslocationsmodule
{
    public Centralcontactlist? CentralContactList { get; set; }
    public Overallofficiallist? OverallOfficialList { get; set; }
    public Locationlist? LocationList { get; set; }
}

public class Centralcontactlist
{
    public Centralcontact[]? CentralContact { get; set; }
}

public class Centralcontact
{
    public string? CentralContactName { get; set; }
    public string? CentralContactRole { get; set; }
    public string? CentralContactPhone { get; set; }
    public string? CentralContactPhoneExt { get; set; }
    public string? CentralContactEMail { get; set; }
}

public class Overallofficiallist
{
    public Overallofficial[]? OverallOfficial { get; set; }
}

public class Overallofficial
{
    public string? OverallOfficialName { get; set; }
    public string? OverallOfficialAffiliation { get; set; }
    public string? OverallOfficialRole { get; set; }
}

public class Locationlist
{
    public Location[]? Location { get; set; }
}

public class Location
{
    public string? LocationFacility { get; set; }
    public string? LocationStatus { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationZip { get; set; }
    public string? LocationCountry { get; set; }
    public Locationcontactlist? LocationContactList { get; set; }
}

public class Locationcontactlist
{
    public Locationcontact[]? LocationContact { get; set; }
}

public class Locationcontact
{
    public string? LocationContactName { get; set; }
    public string? LocationContactRole { get; set; }
    public string? LocationContactPhone { get; set; }
    public string? LocationContactPhoneExt { get; set; }
    public string? LocationContactEMail { get; set; }
}


public class Referencesmodule
{
    public Referencelist? ReferenceList { get; set; }
    public Seealsolinklist? SeeAlsoLinkList { get; set; }
    public Availipdlist? AvailIPDList { get; set; }
}

public class Referencelist
{
    public Reference[]? Reference { get; set; }
}

public class Reference
{
    public string? ReferencePMID { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceCitation { get; set; }
    public Retractionlist? RetractionList { get; set; }
}

public class Retractionlist
{
    public Retraction[]? Retraction { get; set; }
}

public class Retraction
{
    public object? type { get; set; }
    public string? RetractionPMID { get; set; }
    public string? RetractionSource { get; set; }
}

public class Seealsolinklist
{
    public Seealsolink[]? SeeAlsoLink { get; set; }
}

public class Seealsolink
{
    public string? SeeAlsoLinkLabel { get; set; }
    public string? SeeAlsoLinkURL { get; set; }
}

public class Availipdlist
{
    public Availipd[]? AvailIPD { get; set; }
}

public class Availipd
{
    public string? AvailIPDId { get; set; }
    public string? AvailIPDType { get; set; }
    public string? AvailIPDURL { get; set; }
    public string? AvailIPDComment { get; set; }
}


public class Ipdsharingstatementmodule
{
    public string? IPDSharing { get; set; }
    public string? IPDSharingDescription { get; set; }
    public IPDsharinginfotypelist? IPDSharingInfoTypeList  { get; set; }
    public string? IPDSharingTimeFrame { get; set; }
    public string? IPDSharingAccessCriteria { get; set; }
    public string? IPDSharingURL { get; set; }
}

public class IPDsharinginfotypelist
{
    public string[]? IPDSharingInfoType { get; set; }
}

public class Derivedsection
{
    public Interventionbrowsemodule? InterventionBrowseModule { get; set; }
    public Conditionbrowsemodule? ConditionBrowseModule { get; set; }
}


public class Interventionbrowsemodule
{
    public Interventionmeshlist? InterventionMeshList { get; set; }
}

public class Interventionmeshlist
{
    public Interventionmesh[]? InterventionMesh { get; set; }
}

public class Interventionmesh
{
    public string? InterventionMeshId { get; set; }
    public string? InterventionMeshTerm { get; set; }
}

public class Conditionbrowsemodule
{
    public Conditionmeshlist? ConditionMeshList { get; set; }
}


public class Conditionmeshlist
{
    public Conditionmesh[]? ConditionMesh { get; set; }
}

public class Conditionmesh
{
    public string? ConditionMeshId { get; set; }
    public string? ConditionMeshTerm { get; set; }
}

public class Documentsection
{
    public Largedocumentmodule? LargeDocumentModule { get; set; }
}

public class Largedocumentmodule
{
    public Largedoclist? LargeDocList { get; set; }
}

public class Largedoclist
{
    public Largedoc[]? LargeDoc { get; set; }
}

public class Largedoc
{
    public string? LargeDocTypeAbbrev { get; set; }
    public string? LargeDocHasProtocol { get; set; }
    public string? LargeDocHasSAP { get; set; }
    public string? LargeDocHasICF { get; set; }
    public string? LargeDocLabel { get; set; }
    public string? LargeDocDate { get; set; }
    public string? LargeDocUploadDate { get; set; }
    public string? LargeDocFilename { get; set; }
}
