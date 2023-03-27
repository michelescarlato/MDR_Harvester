using Dapper;
using Npgsql;
namespace MDR_Harvester;

public class StudyTableBuilder
{
    private readonly string _db_conn = "";

    public StudyTableBuilder(string? db_conn)
    {
        if (db_conn is not null)
        {
            _db_conn = db_conn;
        }
    }

    private void Execute_SQL(string sql_string)
    {
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }

    public void create_table_studies(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.studies;
        CREATE TABLE {schema}.studies(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , display_title          VARCHAR         NULL
          , title_lang_code        VARCHAR         NULL default 'en'
          , brief_description      VARCHAR         NULL
          , data_sharing_statement VARCHAR         NULL
          , study_start_year       INT             NULL
          , study_start_month      INT             NULL
          , study_type_id          INT             NULL
          , study_type             VARCHAR         NULL
          , study_status_id        INT             NULL
          , study_status           VARCHAR         NULL
          , study_enrolment        VARCHAR         NULL
          , study_gender_elig_id   INT             NULL
          , study_gender_elig      VARCHAR         NULL
          , min_age                INT             NULL
          , min_age_units_id       INT             NULL
          , min_age_units          VARCHAR         NULL
          , max_age                INT             NULL
          , max_age_units_id       INT             NULL
          , max_age_units          VARCHAR         NULL
          , iec_level              INT             NULL    
          , datetime_of_data_fetch TIMESTAMPTZ     NULL
        );
        CREATE INDEX studies_sid ON {schema}.studies(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_identifiers(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_identifiers;
        CREATE TABLE {schema}.study_identifiers(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , identifier_type_id     INT             NULL
          , identifier_type        VARCHAR         NULL
          , identifier_value       VARCHAR         NULL          
          , identifier_org_id      INT             NULL
          , identifier_org         VARCHAR         NULL
          , identifier_date        VARCHAR         NULL
          , identifier_link        VARCHAR         NULL
        );
        CREATE INDEX study_identifiers_sd_sid ON {schema}.study_identifiers(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_relationships(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_relationships;
        CREATE TABLE {schema}.study_relationships(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , relationship_type_id   INT             NULL
          , relationship_type      VARCHAR         NULL
          , target_sd_sid          VARCHAR         NULL
        );
        CREATE INDEX study_relationships_sd_sid ON {schema}.study_relationships(sd_sid);
        CREATE INDEX study_relationships_target_sd_sid ON {schema}.study_relationships(target_sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_references(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_references;
        CREATE TABLE {schema}.study_references(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , citation               VARCHAR         NULL
          , pmid                   VARCHAR         NULL
          , doi                    VARCHAR         NULL	
          , type_id                INT             NULL
          , type                   VARCHAR         NULL
          , comments               VARCHAR         NULL
        );
        CREATE INDEX study_references_sd_sid ON {schema}.study_references(sd_sid);";

        Execute_SQL(sql_string);
    }

    public void create_table_study_titles(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_titles;
        CREATE TABLE {schema}.study_titles(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , title_type_id          INT             NULL
          , title_type             VARCHAR         NULL
          , title_text             VARCHAR         NULL
          , lang_code              VARCHAR         NOT NULL default 'en'
          , lang_usage_id          INT             NOT NULL default 11
          , is_default             BOOLEAN         NULL
          , comments               VARCHAR         NULL
        );
        CREATE INDEX study_titles_sd_sid ON {schema}.study_titles(sd_sid);";

        Execute_SQL(sql_string);
    }

    public void create_table_study_people(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_people;
        CREATE TABLE {schema}.study_people(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , contrib_type_id        INT             NULL
          , contrib_type           VARCHAR         NULL
          , person_given_name      VARCHAR         NULL
          , person_family_name     VARCHAR         NULL
          , person_full_name       VARCHAR         NULL
          , orcid_id               VARCHAR         NULL
          , person_affiliation     VARCHAR         NULL
          , organisation_id        INT             NULL
          , organisation_name      VARCHAR         NULL
        );
        CREATE INDEX _study_people_sd_sid ON {schema}.study_people(sd_sid);";

        Execute_SQL(sql_string);
    }
    
    public void create_table_study_organisations(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_organisations;
        CREATE TABLE {schema}.study_organisations(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , contrib_type_id        INT             NULL
          , contrib_type           VARCHAR         NULL
          , organisation_id        INT             NULL
          , organisation_name      VARCHAR         NULL
        );
        CREATE INDEX study_organisations_sd_sid ON {schema}.study_organisations(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_topics(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_topics;
        CREATE TABLE {schema}.study_topics(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , topic_type_id          INT             NULL
          , topic_type             VARCHAR         NULL
          , original_value         VARCHAR         NULL 
          , original_ct_type_id    INT             NULL
          , original_ct_type       VARCHAR         NULL
          , original_ct_code       VARCHAR         NULL 
          , mesh_code              VARCHAR         NULL
          , mesh_value             VARCHAR         NULL
        );
        CREATE INDEX study_topics_sd_sid ON {schema}.study_topics(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_conditions(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_conditions;
        CREATE TABLE {schema}.study_conditions(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , original_value         VARCHAR         NULL
          , original_ct_type_id    INT             NULL
          , original_ct_type       VARCHAR         NULL    
          , original_ct_code       VARCHAR         NULL                 
          , icd_code               VARCHAR         NULL
          , icd_name               VARCHAR         NULL
        );
        CREATE INDEX study_conditions_sd_sid ON {schema}.study_conditions(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_features(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_features;
        CREATE TABLE {schema}.study_features(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , feature_type_id        INT             NULL
          , feature_type           VARCHAR         NULL
          , feature_value_id       INT             NULL
          , feature_value          VARCHAR         NULL
        );
        CREATE INDEX study_features_sd_sid ON {schema}.study_features(sd_sid);";

        Execute_SQL(sql_string);
    }

    
    public void create_table_study_links(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_links;
        CREATE TABLE {schema}.study_links(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , link_label             VARCHAR         NULL
          , link_url               VARCHAR         NULL
        );
        CREATE INDEX study_links_sd_sid ON {schema}.study_links(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_locations(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_locations;
        CREATE TABLE {schema}.study_locations(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , facility_org_id        INT             NULL
          , facility               VARCHAR         NULL
          , city_id                INT             NULL
          , city_name              VARCHAR         NULL
          , country_id             INT             NULL
          , country_name           VARCHAR         NULL
          , status_id              INT             NULL
          , status                 VARCHAR         NULL
        );
        CREATE INDEX study_locations_sd_sid ON {schema}.study_locations(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_study_countries(string schema)
    {  
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_countries;
        CREATE TABLE {schema}.study_countries(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , country_id             INT             NULL
          , country_name           VARCHAR         NULL
          , status_id              INT             NULL
          , status                 VARCHAR         NULL
        );
        CREATE INDEX study_countries_sd_sid ON {schema}.study_countries(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_ipd_available(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.study_ipd_available;
        CREATE TABLE {schema}.study_ipd_available(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , ipd_id                 VARCHAR         NULL
          , ipd_type               VARCHAR         NULL
          , ipd_url                VARCHAR         NULL
          , ipd_comment            VARCHAR         NULL
        );
        CREATE INDEX study_ipd_available_sd_sid ON {schema}.study_ipd_available(sd_sid);";

        Execute_SQL(sql_string);
    }
    
    private void create_iec_table(string schema, string table_name)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.{table_name};
        CREATE TABLE {schema}.{table_name}(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_sid                 VARCHAR         NOT NULL
          , seq_num                INT             NULL        
          , iec_type_id            INT             NULL
          , iec_type               VARCHAR         NULL              
          , split_type             VARCHAR         NULL
          , leader                 VARCHAR         NULL
          , indent_level           INT             NULL
          , level_seq_num          INT             NULL
          , sequence_string        VARCHAR         NULL
          , iec_text               VARCHAR         NULL
        );
        CREATE INDEX {table_name}_sid ON {schema}.{table_name}(sd_sid);";

        Execute_SQL(sql_string);
    }

    public void create_table_study_iec(string schema)
    {
        create_iec_table(schema, "study_iec");
    }
    
    public void create_table_study_iec_by_year_groups(string schema)
    {
        create_iec_table(schema, "study_iec_upto12");
        create_iec_table(schema, "study_iec_13to19");
        create_iec_table(schema, "study_iec_20on");
    }

    public void create_table_study_iec_by_years(string schema)
    {
        create_iec_table(schema, "study_iec_null");
        create_iec_table(schema, "study_iec_pre06");
        create_iec_table(schema, "study_iec_0608");
        create_iec_table(schema, "study_iec_0910");
        create_iec_table(schema, "study_iec_1112");
        create_iec_table(schema, "study_iec_1314");
        for (int i = 15; i <= 30; i++)
        {
            create_iec_table(schema, $"study_iec_{i}");
        }
    }
    
}