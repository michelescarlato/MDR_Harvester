using PostgreSQLCopyHelper;

namespace MDR_Harvester
{
    public class StudyCopyHelpers : IStudyCopyHelpers
    {
        public PostgreSQLCopyHelper<StudyIdentifier> studyIdentifiersHelper => study_ids_helper;
        public PostgreSQLCopyHelper<StudyTitle> studyTitlesHelper =>  study_titles_helper;
        public PostgreSQLCopyHelper<StudyTopic> studyTopicsHelper =>  study_topics_helper;
        public PostgreSQLCopyHelper<StudyCondition> studyConditionsHelper =>  study_conditions_helper;
        public PostgreSQLCopyHelper<StudyIEC> studyIECHelper =>  study_iec_helper;
        public PostgreSQLCopyHelper<StudyContributor> studyContributorsHelper =>  study_contributors_helper;
        public PostgreSQLCopyHelper<StudyRelationship> studyRelationshipsHelper =>  study_relationships_helper;
        public PostgreSQLCopyHelper<StudyLink> studyLinksHelper =>  study_links_helper;
        public PostgreSQLCopyHelper<StudyFeature> studyFeaturesHelper =>  study_features_helper;
        public PostgreSQLCopyHelper<StudyReference> studyReferencesHelper =>  study_references_helper;
        public PostgreSQLCopyHelper<StudyLocation> studyLocationsHelper =>  study_locations_helper;
        public PostgreSQLCopyHelper<StudyCountry> studyCountriesHelper =>  study_countries_helper;
        public PostgreSQLCopyHelper<AvailableIPD> studyAvailIPDHelper =>  study_avail_ipd_helper;
 
        
        private PostgreSQLCopyHelper<StudyIdentifier> study_ids_helper =
            new PostgreSQLCopyHelper<StudyIdentifier>("sd", "study_identifiers")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("identifier_value", x => x.identifier_value)
                .MapInteger("identifier_type_id", x => x.identifier_type_id)
                .MapVarchar("identifier_type", x => x.identifier_type)
                .MapInteger("identifier_org_id", x => x.identifier_org_id)
                .MapVarchar("identifier_org", x => x.identifier_org)
                .MapVarchar("identifier_org_ror_id", x => x.identifier_org_ror_id)
                .MapVarchar("identifier_date", x => x.identifier_date)
                .MapVarchar("identifier_link", x => x.identifier_link);


        private PostgreSQLCopyHelper<StudyTitle> study_titles_helper =
            new PostgreSQLCopyHelper<StudyTitle>("sd", "study_titles")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapInteger("title_type_id", x => x.title_type_id)
                .MapVarchar("title_type", x => x.title_type)
                .MapVarchar("title_text", x => x.title_text)
                .MapBoolean("is_default", x => x.is_default)
                .MapVarchar("lang_code", x => x.lang_code)
                .MapInteger("lang_usage_id", x => x.lang_usage_id)
                .MapVarchar("comments", x => x.comments);


        private PostgreSQLCopyHelper<StudyTopic> study_topics_helper =
            new PostgreSQLCopyHelper<StudyTopic>("sd", "study_topics")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapInteger("topic_type_id", x => x.topic_type_id)
                .MapVarchar("topic_type", x => x.topic_type)
                .MapBoolean("mesh_coded", x => x.mesh_coded)
                .MapVarchar("mesh_code", x => x.mesh_code)
                .MapVarchar("mesh_value", x => x.mesh_value)
                .MapInteger("original_ct_id", x => x.original_ct_id)
                .MapVarchar("original_ct_code", x => x.original_ct_code)
                .MapVarchar("original_value", x => x.original_value);


        private PostgreSQLCopyHelper<StudyCondition> study_conditions_helper =
            new PostgreSQLCopyHelper<StudyCondition>("sd", "study_conditions")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("original_value", x => x.original_value)
                .MapInteger("original_ct", x => x.original_ct)
                .MapVarchar("original_ct_code", x => x.original_ct_code)               
                .MapVarchar("icd_code", x => x.icd_code)
                .MapVarchar("icd_name", x => x.icd_name);


        private PostgreSQLCopyHelper<StudyIEC> study_iec_helper =
            new PostgreSQLCopyHelper<StudyIEC>("sd", "study_iec")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapInteger("seq_num", x => x.seq_num)
                .MapVarchar("leader", x => x.leader)
                .MapInteger("indent_level", x => x.indent_level)
                .MapInteger("level_seq_num", x => x.level_seq_num)
                .MapInteger("iec_type_id", x => x.iec_type_id)
                .MapVarchar("iec_type", x => x.iec_type)
                .MapVarchar("iec_text", x => x.iec_text);


        private PostgreSQLCopyHelper<StudyContributor> study_contributors_helper =
            new PostgreSQLCopyHelper<StudyContributor>("sd", "study_contributors")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapInteger("contrib_type_id", x => x.contrib_type_id)
                .MapVarchar("contrib_type", x => x.contrib_type)
                .MapBoolean("is_individual", x => x.is_individual)
                .MapInteger("person_id", x => x.person_id)
                .MapVarchar("person_given_name", x => x.person_given_name)
                .MapVarchar("person_family_name", x => x.person_family_name)
                .MapVarchar("person_full_name", x => x.person_full_name)
                .MapVarchar("orcid_id", x => x.orcid_id)
                .MapVarchar("person_affiliation", x => x.person_affiliation)
                .MapInteger("organisation_id", x => x.organisation_id)
                .MapVarchar("organisation_name", x => x.organisation_name)
                .MapVarchar("organisation_ror_id", x => x.organisation_ror_id);


        private PostgreSQLCopyHelper<StudyRelationship> study_relationships_helper =
            new PostgreSQLCopyHelper<StudyRelationship>("sd", "study_relationships")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapInteger("relationship_type_id", x => x.relationship_type_id)
                .MapVarchar("relationship_type", x => x.relationship_type)
                .MapVarchar("target_sd_sid", x => x.target_sd_sid);


        private PostgreSQLCopyHelper<StudyLink> study_links_helper =
            new PostgreSQLCopyHelper<StudyLink>("sd", "study_links")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("link_label", x => x.link_label)
                .MapVarchar("link_url", x => x.link_url);


        private PostgreSQLCopyHelper<StudyFeature> study_features_helper =
            new PostgreSQLCopyHelper<StudyFeature>("sd", "study_features")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapInteger("feature_type_id", x => x.feature_type_id)
                .MapVarchar("feature_type", x => x.feature_type)
                .MapInteger("feature_value_id", x => x.feature_value_id)
                .MapVarchar("feature_value", x => x.feature_value);


        private PostgreSQLCopyHelper<StudyReference> study_references_helper =
            new PostgreSQLCopyHelper<StudyReference>("sd", "study_references")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("pmid", x => x.pmid)
                .MapVarchar("citation", x => x.citation)
                .MapVarchar("doi", x => x.doi)
                .MapInteger("type_id", x => x.type_id)
                .MapVarchar("type", x => x.type)
                .MapVarchar("comments", x => x.comments);


        private PostgreSQLCopyHelper<StudyLocation> study_locations_helper =
            new PostgreSQLCopyHelper<StudyLocation>("sd", "study_locations")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("facility", x => x.facility)
                .MapVarchar("city_name", x => x.city_name)
                .MapVarchar("country_name", x => x.country_name)
                .MapInteger("status_id", x => x.status_id)
                .MapVarchar("status", x => x.status);


        private PostgreSQLCopyHelper<StudyCountry> study_countries_helper =
            new PostgreSQLCopyHelper<StudyCountry>("sd", "study_countries")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("country_name", x => x.country_name)
                .MapInteger("status_id", x => x.status_id)
                .MapVarchar("status", x => x.status);


        private PostgreSQLCopyHelper<AvailableIPD> study_avail_ipd_helper =
            new PostgreSQLCopyHelper<AvailableIPD>("sd", "study_ipd_available")
                .MapVarchar("sd_sid", x => x.sd_sid)
                .MapVarchar("ipd_id", x => x.ipd_id)
                .MapVarchar("ipd_type", x => x.ipd_type)
                .MapVarchar("ipd_url", x => x.ipd_url)
                .MapVarchar("ipd_comment", x => x.ipd_comment);
    }
}
