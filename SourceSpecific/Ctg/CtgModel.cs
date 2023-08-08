
namespace MDR_Harvester.Ctg;


public class CTG_Record
{
    public ProtocolSection? protocolSection { get; set; }
    public DerivedSection? derivedSection { get; set; }
    public DocumentSection? documentSection { get; set; }
    public bool? hasResults { get; set; }
}

public class ProtocolSection
{
    public IdentificationModule? identificationModule { get; set; }
    public StatusModule? statusModule { get; set; }
    public SponsorCollaboratorsModule? sponsorCollaboratorsModule { get; set; }
    public OversightModule? oversightModule { get; set; }
    public DescriptionModule? descriptionModule { get; set; }
    public ConditionsModule? conditionsModule { get; set; }
    public DesignModule? designModule { get; set; }
    public EligibilityModule? eligibilityModule { get; set; }
    public ContactsLocationsModule? contactsLocationsModule { get; set; }    
    public ReferencesModule? referencesModule { get; set; }
    public IPDSharingStatementModule? ipdSharingStatementModule { get; set; }
}

public class IdentificationModule
{
    public string? nctId { get; set; }
    public string[]? nctIdAliases { get; set; }   
    public OrgStudyIdInfo? orgStudyIdInfo { get; set; }    
    public SecondaryIdInfos[]? secondaryIdInfos { get; set; }    
    public string? briefTitle { get; set; }
    public string? officialTitle { get; set; }
    public string? acronym { get; set; }
    public Organization? organization { get; set; }
}

public class OrgStudyIdInfo
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? link { get; set; }
}

public class SecondaryIdInfos
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? domain { get; set; }    
    public string? link { get; set; }
}


public class Organization
{
    public string? fullName { get; set; }
    public string? ctg_class { get; set; }
}

public class StatusModule
{
    public string? statusVerifiedDate { get; set; }
    public string? overallStatus { get; set; }
    public string? lastKnownStatus { get; set; }
    public string? whyStopped { get; set; }
    public ExpandedAccessInfo? expandedAccessInfo { get; set; }
    
    public StartDateStruct? startDateStruct { get; set; }
    public PrimaryCompletionDateStruct? primaryCompletionDateStruct { get; set; }
    public CompletionDateStruct? completionDateStruct { get; set; }
    public string? studyFirstSubmitDate { get; set; }
    public StudyFirstPostDateStruct? studyFirstPostDateStruct { get; set; }
    public string? resultsFirstSubmitDate { get; set; }
    public ResultsFirstPostDateStruct? resultsFirstPostDateStruct { get; set; }
    public string? lastUpdateSubmitDate { get; set; }
    public LastUpdatePostDateStruct? lastUpdatePostDateStruct { get; set; }
}

public class ExpandedAccessInfo
{
    public bool hasExpandedAccess { get; set; }
    public string? nctId { get; set; }
    public string? statusForNctId { get; set; }
}

public class StartDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class CompletionDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class StudyFirstPostDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class LastUpdatePostDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class PrimaryCompletionDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class ResultsFirstPostDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class SponsorCollaboratorsModule
{
    public ResponsibleParty? responsibleParty { get; set; }        
    public LeadSponsor? leadSponsor { get; set; }
    public Collaborator[]? collaborators { get; set; }
}

public class LeadSponsor
{
    public string? name { get; set; }
    public string? ctg_class { get; set; }
}

public class ResponsibleParty
{
    public string? type { get; set; }
    public string? investigatorFullName { get; set; }
    public string? investigatorTitle { get; set; }
    public string? investigatorAffiliation { get; set; }
    public string? oldNameTitle { get; set; }
    public string? oldOrganization { get; set; }

}

public class Collaborator
{
    public string? name { get; set; }
    public string? ctg_class { get; set; }
}

public class OversightModule
{
    public bool? oversightHasDmc { get; set; }
    public bool? isFdaRegulatedDrug { get; set; }
    public bool? isFdaRegulatedDevice { get; set; }
    public bool? isUnapprovedDevice { get; set; }
    public bool? isPpsd { get; set; }
}

public class DescriptionModule
{
    public string? briefSummary { get; set; }
    public string? detailedDescription { get; set; }
}

public class ConditionsModule
{
    public string[]? conditions { get; set; }
    public string[]? keywords { get; set; }
}

public class DesignModule
{
    public string? studyType { get; set; }
    public bool? patientRegistry { get; set; }      
    public string[]? phases { get; set; }        
    public DesignInfo? designInfo { get; set; }
    public EnrollmentInfo? enrollmentInfo { get; set; }
    public Biospec? bioSpec { get; set; }
}

public class DesignInfo
{
    public string? allocation { get; set; }
    public string? interventionModel { get; set; }    
    public string? interventionModelDescription { get; set; }    
    public string? primaryPurpose { get; set; }
    public string? observationalModel { get; set; }
    public string? timePerspective { get; set; }
    public MaskingInfo? maskingInfo { get; set; }
}

public class MaskingInfo
{
    public string? masking { get; set; }
    public string? maskingDescription { get; set; }
    public string[]? whoMasked { get; set; }
    public int? numDesignWhoMaskeds  { get; set; }
}

public class EnrollmentInfo
{
    public int? count { get; set; }
    public string? type { get; set; }
}

public class Biospec
{
    public string? retention { get; set; }
    public string? description { get; set; }
}

public class EligibilityModule
{
    public string? eligibilityCriteria { get; set; }
    public bool healthyVolunteers { get; set; }
    public string? sex { get; set; }
    public bool genderBased { get; set; }
    public string? genderDescription { get; set; }
    public string? minimumAge { get; set; }
    public string[]? stdAges { get; set; }
    public string? maximumAge { get; set; }
    public string? studyPopulation { get; set; }
    public string? samplingMethod { get; set; }
}

public class ContactsLocationsModule
{
    public Centralcontact[]? centralContacts { get; set; }
    public OverallOfficial[]? overallOfficials { get; set; }
    public Location[]? locations { get; set; }
}

public class Centralcontact
{
    public string? name { get; set; }
    public string? role { get; set; }
    public string? email { get; set; }
}

public class OverallOfficial
{
    public string? name { get; set; }
    public string? affiliation { get; set; }
    public string? role { get; set; }
}

public class Location
{
    public string? facility { get; set; }
    public string? city { get; set; }
    public string? country { get; set; }
    public GeoPoint? geoPoint { get; set; }
    public string? state { get; set; }
    public string? status { get; set; } 
}

public class GeoPoint
{
    public double? lat { get; set; }
    public double? lon { get; set; }
}

public class ReferencesModule
{
    public References[]? references { get; set; }
    public SeeAlsoLinks[]? seeAlsoLinks { get; set; }
    public AvailIpd[]? availIpds { get; set; }
}

public class References
{
    public string? pmid { get; set; }
    public string? type { get; set; }
    public string? citation { get; set; }
    public Retraction[]? retractions { get; set; }
}

public class Retraction
{
    public string? pmid { get; set; }
    public string? source { get; set; }
}

public class SeeAlsoLinks
{
    public string? label { get; set; }
    public string? url { get; set; }
}

public class AvailIpd
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? url { get; set; }
    public string? comment { get; set; }
}

public class IPDSharingStatementModule
{
    public string? ipdSharing { get; set; }
    public string? description { get; set; }
    public string[]? infoTypes { get; set; }
    public string? timeFrame { get; set; }
    public string? accessCriteria { get; set; }
    public string? url { get; set; }
}

public class DocumentSection
{
    public LargeDocumentModule? largeDocumentModule { get; set; }
}

public class LargeDocumentModule
{
    public LargeDocs[]? largeDocs { get; set; }
}

public class LargeDocs
{
    public string? typeAbbrev { get; set; }
    public bool? hasProtocol { get; set; }
    public bool? hasSap { get; set; }
    public bool? hasIcf { get; set; }
    public string? label { get; set; }
    public string? date { get; set; }
    public string? uploadDate { get; set; }
    public string? filename { get; set; }
    public int? size { get; set; }
}

public class DerivedSection
{
    public ConditionBrowseModule? conditionBrowseModule { get; set; }
    public InterventionBrowseModule? interventionBrowseModule { get; set; }
}

public class ConditionBrowseModule
{
    public Mesh[]? meshes { get; set; }
}

public class InterventionBrowseModule
{
    public Mesh[]? meshes { get; set; }
}

public class Mesh
{
    public string? id { get; set; }
    public string? term { get; set; }
}
    

/*
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

*/


