using Dapper;
using Npgsql;

namespace MDR_Harvester;

class StudyTablesTransferrer
{
    private readonly string _source_id;
    private readonly string _db_conn;

    public StudyTablesTransferrer(int source_id, string db_conn)
    {
        _source_id = source_id.ToString();
        _db_conn = db_conn;
    }

    private void Execute_SQL(string sql_string)
    {
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }
    
    public void TransferStudies()
    {
        string sql_string = @"INSERT INTO sdcomp.studies (source_id, sd_sid, display_title,
        title_lang_code, brief_description, data_sharing_statement,
        study_start_year, study_start_month, study_type_id, study_type,
        study_status_id, study_status, study_enrolment, study_gender_elig_id, study_gender_elig, 
        min_age, min_age_units_id, min_age_units, 
        max_age, max_age_units_id, max_age_units, datetime_of_data_fetch,
        record_hash, study_full_hash) 
        SELECT " + _source_id + @", sd_sid, display_title,
        title_lang_code, brief_description, data_sharing_statement,
        study_start_year, study_start_month, study_type_id, study_type,
        study_status_id, study_status, study_enrolment, study_gender_elig_id, study_gender_elig,  
        min_age, min_age_units_id, min_age_units, 
        max_age, max_age_units_id, max_age_units, datetime_of_data_fetch,
        record_hash, study_full_hash 
        FROM sd.studies";

        Execute_SQL(sql_string);

    }


    public void TransferStudyIdentifiers()
    {
        string sql_string = @"INSERT INTO sdcomp.study_identifiers(source_id, sd_sid,
        identifier_value, identifier_type_id, identifier_type, identifier_org_id, 
        identifier_org, identifier_org_ror_id, identifier_date, identifier_link, record_hash)
        SELECT " + _source_id + @", sd_sid,
        identifier_value, identifier_type_id, identifier_type, identifier_org_id, 
        identifier_org, identifier_org_ror_id, identifier_date, identifier_link, record_hash
        FROM sd.study_identifiers";

        Execute_SQL(sql_string);

    }


    public void TransferStudyRelationships()
    {

        string sql_string = @"INSERT INTO sdcomp.study_relationships(source_id, sd_sid,
        relationship_type_id, relationship_type, target_sd_sid, record_hash)
        SELECT " + _source_id + @", sd_sid,
        relationship_type_id, relationship_type, target_sd_sid, record_hash
        FROM sd.study_relationships";

        Execute_SQL(sql_string);
    }


    public void TransferStudyReferences()
    {

        string sql_string = @"INSERT INTO sdcomp.study_references(source_id, sd_sid,
        pmid, citation, doi, comments, record_hash)
        SELECT " + _source_id + @", sd_sid,
        pmid, citation, doi, comments, record_hash
        FROM sd.study_references";

        Execute_SQL(sql_string);
    }


    public void TransferStudyTitles()
    {   
        
        string sql_string = @"INSERT INTO sdcomp.study_titles(source_id, sd_sid,
        title_type_id, title_type, title_text, lang_code, lang_usage_id,
        is_default, comments, record_hash)
        SELECT " + _source_id + @", sd_sid,
        title_type_id, title_type, title_text, lang_code, lang_usage_id,
        is_default, comments, record_hash
        FROM sd.study_titles";

        Execute_SQL(sql_string);

    }


    public void TransferStudyPeople()
    {
        string sql_string = @"INSERT INTO sdcomp.study_people(source_id, sd_sid, 
        contrib_type_id, contrib_type, 
        person_id, person_given_name, person_family_name, person_full_name,
        orcid_id, person_affiliation, organisation_id, 
        organisation_name, organisation_ror_id, record_hash)
        SELECT " + _source_id + @", sd_sid,
        contrib_type_id, contrib_type, 
        person_id, person_given_name, person_family_name, person_full_name,
        orcid_id, person_affiliation, organisation_id, 
        organisation_name, organisation_ror_id, record_hash
        FROM sd.study_contributors";

        Execute_SQL(sql_string);

    }
    
    public void TransferStudyOrganisations()
    {
        string sql_string = @"INSERT INTO sdcomp.study_organisations(source_id, sd_sid, 
        contrib_type_id, contrib_type, organisation_id, 
        organisation_name, organisation_ror_id, record_hash)
        SELECT " + _source_id + @", sd_sid,
        contrib_type_id, contrib_type, organisation_id, 
        organisation_name, organisation_ror_id, record_hash
        FROM sd.study_contributors";

        Execute_SQL(sql_string);

    }


    public void TransferStudyTopics()
    {
        string sql_string = @"INSERT INTO sdcomp.study_topics(source_id, sd_sid,
        topic_type_id, topic_type, mesh_coded, mesh_code, mesh_value, 
        original_ct_id, original_ct_code,
        original_value, record_hash)
        SELECT " + _source_id + @", sd_sid,
        topic_type_id, topic_type, mesh_coded, mesh_code, mesh_value, 
        original_ct_id, original_ct_code,
        original_value, record_hash
        FROM sd.study_topics";

        Execute_SQL(sql_string);
    }


    public void TransferStudyFeatures()
    {
        string sql_string = @"INSERT INTO sdcomp.study_features(source_id, sd_sid,
        feature_type_id, feature_type, feature_value_id, feature_value, record_hash)
        SELECT " + _source_id + @", sd_sid,
        feature_type_id, feature_type, feature_value_id, feature_value, record_hash
        FROM sd.study_features";

        Execute_SQL(sql_string);
    }


    public void TransferStudyLinks()
    {
         
        string sql_string = @"INSERT INTO sdcomp.study_links(source_id, sd_sid,
        link_label, link_url, record_hash)
        SELECT " + _source_id + @", sd_sid,
        link_label, link_url, record_hash
        FROM sd.study_links";

        Execute_SQL(sql_string);
    }


    public void TransferStudyIPDAvailable()
    {
        string sql_string = @"INSERT INTO sdcomp.study_ipd_available(source_id, sd_sid,
        ipd_id, ipd_type, ipd_url, ipd_comment, record_hash)
        SELECT " + _source_id + @", sd_sid,
        ipd_id, ipd_type, ipd_url, ipd_comment, record_hash
        FROM sd.study_ipd_available";

        Execute_SQL(sql_string);

    }

    public void TransferStudyHashes()
    {
        string sql_string = @"INSERT INTO sdcomp.study_hashes(source_id, sd_sid,
        hash_type_id, hash_type, composite_hash)
        SELECT " + _source_id + @", sd_sid,
        hash_type_id, hash_type, composite_hash
        FROM sd.study_hashes;";

        Execute_SQL(sql_string);
    }

}