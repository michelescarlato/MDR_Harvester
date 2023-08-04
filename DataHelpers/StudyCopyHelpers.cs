using PostgreSQLCopyHelper;

namespace MDR_Harvester;

public class StudyCopyHelpers : IStudyCopyHelpers
{
    // Class is a set of auto-implemented properties, each of which is
    // initialised to the PostgreSQLCopyHelper<T>("schema", "table_name") shown.
    
    public PostgreSQLCopyHelper<StudyIdentifier> studyIdentifiersHelper { get; } = 
        new PostgreSQLCopyHelper<StudyIdentifier>("sd", "study_identifiers")
        .MapVarchar("sd_sid", x => x.sd_sid)
        .MapVarchar("identifier_value", x => x.identifier_value)
        .MapInteger("identifier_type_id", x => x.identifier_type_id)
        .MapVarchar("identifier_type", x => x.identifier_type)
        .MapInteger("source_id", x => x.source_id)
        .MapVarchar("source", x => x.source)
        .MapVarchar("identifier_date", x => x.identifier_date)
        .MapVarchar("identifier_link", x => x.identifier_link);

    public PostgreSQLCopyHelper<StudyTitle> studyTitlesHelper { get; } =
        new PostgreSQLCopyHelper<StudyTitle>("sd", "study_titles")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("title_type_id", x => x.title_type_id)
            .MapVarchar("title_type", x => x.title_type)
            .MapVarchar("title_text", x => x.title_text)
            .MapBoolean("is_default", x => x.is_default)
            .MapVarchar("lang_code", x => x.lang_code)
            .MapInteger("lang_usage_id", x => x.lang_usage_id)
            .MapVarchar("comments", x => x.comments);

    public PostgreSQLCopyHelper<StudyTopic> studyTopicsHelper { get; } =
        new PostgreSQLCopyHelper<StudyTopic>("sd", "study_topics")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("topic_type_id", x => x.topic_type_id)
            .MapVarchar("topic_type", x => x.topic_type)
            .MapVarchar("original_value", x => x.original_value)           
            .MapInteger("original_ct_type_id", x => x.original_ct_type_id)
            .MapVarchar("original_ct_type", x => x.original_ct_type)
            .MapVarchar("original_ct_code", x => x.original_ct_code)           
            .MapVarchar("mesh_code", x => x.mesh_code)
            .MapVarchar("mesh_value", x => x.mesh_value);
    
    public PostgreSQLCopyHelper<StudyCondition> studyConditionsHelper { get; } =
        new PostgreSQLCopyHelper<StudyCondition>("sd", "study_conditions")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapVarchar("original_value", x => x.original_value)
            .MapInteger("original_ct_type_id", x => x.original_ct_type_id)
            .MapVarchar("original_ct_type", x => x.original_ct_type)
            .MapVarchar("original_ct_code", x => x.original_ct_code)
            .MapVarchar("icd_code", x => x.icd_code)
            .MapVarchar("icd_name", x => x.icd_name);

    public PostgreSQLCopyHelper<StudyPerson> studyPeopleHelper { get; } =
        new PostgreSQLCopyHelper<StudyPerson>("sd", "study_people")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("contrib_type_id", x => x.contrib_type_id)
            .MapVarchar("contrib_type", x => x.contrib_type)
            .MapVarchar("person_given_name", x => x.person_given_name)
            .MapVarchar("person_family_name", x => x.person_family_name)
            .MapVarchar("person_full_name", x => x.person_full_name)
            .MapVarchar("orcid_id", x => x.orcid_id)
            .MapVarchar("person_affiliation", x => x.person_affiliation)
            .MapInteger("organisation_id", x => x.organisation_id)
            .MapVarchar("organisation_name", x => x.organisation_name);

    public PostgreSQLCopyHelper<StudyOrganisation> studyOrganisationsHelper { get; } =
        new PostgreSQLCopyHelper<StudyOrganisation>("sd", "study_organisations")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("contrib_type_id", x => x.contrib_type_id)
            .MapVarchar("contrib_type", x => x.contrib_type)
            .MapInteger("organisation_id", x => x.organisation_id)
            .MapVarchar("organisation_name", x => x.organisation_name);

    public PostgreSQLCopyHelper<StudyRelationship> studyRelationshipsHelper { get; } =
        new PostgreSQLCopyHelper<StudyRelationship>("sd", "study_relationships")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("relationship_type_id", x => x.relationship_type_id)
            .MapVarchar("relationship_type", x => x.relationship_type)
            .MapVarchar("target_sd_sid", x => x.target_sd_sid);

    public PostgreSQLCopyHelper<StudyLink> studyLinksHelper { get; } =
        new PostgreSQLCopyHelper<StudyLink>("sd", "study_links")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapVarchar("link_label", x => x.link_label)
            .MapVarchar("link_url", x => x.link_url);

    public PostgreSQLCopyHelper<StudyFeature> studyFeaturesHelper { get; } =
        new PostgreSQLCopyHelper<StudyFeature>("sd", "study_features")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("feature_type_id", x => x.feature_type_id)
            .MapVarchar("feature_type", x => x.feature_type)
            .MapInteger("feature_value_id", x => x.feature_value_id)
            .MapVarchar("feature_value", x => x.feature_value);

    public PostgreSQLCopyHelper<StudyReference> studyReferencesHelper { get; } =
        new PostgreSQLCopyHelper<StudyReference>("sd", "study_references")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapVarchar("pmid", x => x.pmid)
            .MapVarchar("citation", x => x.citation)
            .MapVarchar("doi", x => x.doi)
            .MapInteger("type_id", x => x.type_id)
            .MapVarchar("type", x => x.type)
            .MapVarchar("comments", x => x.comments);

    public PostgreSQLCopyHelper<StudyLocation> studyLocationsHelper { get; } =
        new PostgreSQLCopyHelper<StudyLocation>("sd", "study_locations")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapVarchar("facility", x => x.facility)
            .MapVarchar("city_name", x => x.city_name)
            .MapVarchar("country_name", x => x.country_name)
            .MapInteger("status_id", x => x.status_id)
            .MapVarchar("status", x => x.status);

    public PostgreSQLCopyHelper<StudyCountry> studyCountriesHelper { get; } =
        new PostgreSQLCopyHelper<StudyCountry>("sd", "study_countries")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapVarchar("country_name", x => x.country_name)
            .MapInteger("status_id", x => x.status_id)
            .MapVarchar("status", x => x.status);

    public PostgreSQLCopyHelper<AvailableIPD> studyAvailIPDHelper { get; } =
        new PostgreSQLCopyHelper<AvailableIPD>("sd", "study_ipd_available")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapVarchar("ipd_id", x => x.ipd_id)
            .MapVarchar("ipd_type", x => x.ipd_type)
            .MapVarchar("ipd_url", x => x.ipd_url)
            .MapVarchar("ipd_comment", x => x.ipd_comment);
    
    public PostgreSQLCopyHelper<StudyIEC> studyIECHelper{ get; } =
        new PostgreSQLCopyHelper<StudyIEC>("sd", "study_iec")
            .MapVarchar("sd_sid", x => x.sd_sid)
            .MapInteger("seq_num", x => x.seq_num)
            .MapInteger("iec_type_id", x => x.iec_type_id)
            .MapVarchar("iec_type", x => x.iec_type)
            .MapVarchar("split_type", x => x.split_type)
            .MapVarchar("leader", x => x.leader)
            .MapInteger("indent_level", x => x.indent_level)
            .MapInteger("level_seq_num", x => x.level_seq_num)
            .MapVarchar("sequence_string", x => x.sequence_string)
            .MapVarchar("iec_text", x => x.iec_text);

}

