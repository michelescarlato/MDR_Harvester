using Dapper;
using Npgsql;

namespace MDR_Harvester;

public class ObjectTableBuilder
{
    private readonly string _db_conn = "";

    public ObjectTableBuilder(string? db_conn)
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

    public void create_table_data_objects(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.data_objects;
          CREATE TABLE {schema}.data_objects(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , sd_sid                 VARCHAR         NULL
          , title                  VARCHAR         NULL
          , version                VARCHAR         NULL
          , display_title          VARCHAR         NULL
          , doi                    VARCHAR         NULL 
          , doi_status_id          INT             NULL
          , publication_year       INT             NULL
          , object_class_id        INT             NULL
          , object_class           VARCHAR         NULL
          , object_type_id         INT             NULL
          , object_type            VARCHAR         NULL
          , managing_org_id        INT             NULL
          , managing_org           VARCHAR         NULL
          , lang_code              VARCHAR         NULL
          , access_type_id         INT             NULL
          , access_type            VARCHAR         NULL
          , access_details         VARCHAR         NULL
          , access_details_url     VARCHAR         NULL
          , url_last_checked       DATE            NULL
          , eosc_category          INT             NULL
          , add_study_contribs     BOOLEAN         NULL
          , add_study_topics       BOOLEAN         NULL
          , datetime_of_data_fetch TIMESTAMPTZ     NULL
        );
        CREATE INDEX data_objects_sd_oid ON {schema}.data_objects(sd_oid);
        CREATE INDEX data_objects_sd_sid ON {schema}.data_objects(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_datasets(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_datasets;
        CREATE TABLE {schema}.object_datasets(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , record_keys_type_id    INT             NULL 
          , record_keys_type       VARCHAR         NULL     	
          , record_keys_details    VARCHAR         NULL    
          , deident_type_id        INT             NULL  
          , deident_type  	       VARCHAR         NULL    
          , deident_direct 	       BOOLEAN         NULL   
          , deident_hipaa 	       BOOLEAN         NULL   
          , deident_dates 	       BOOLEAN         NULL   
          , deident_nonarr 	       BOOLEAN         NULL   
          , deident_kanon	       BOOLEAN         NULL   
          , deident_details        VARCHAR         NULL    
          , consent_type_id        INT             NULL  
          , consent_type           VARCHAR         NULL
          , consent_noncommercial  BOOLEAN         NULL
          , consent_geog_restrict  BOOLEAN         NULL
          , consent_research_type  BOOLEAN         NULL
          , consent_genetic_only   BOOLEAN         NULL
          , consent_no_methods     BOOLEAN         NULL
          , consent_details        VARCHAR         NULL 
        );
        CREATE INDEX object_datasets_sd_oid ON {schema}.object_datasets(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_dates(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_dates;
        CREATE TABLE {schema}.object_dates(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , date_type_id           INT             NULL
          , date_type              VARCHAR         NULL
          , date_is_range          BOOLEAN         NULL default false
          , date_as_string         VARCHAR         NULL
          , start_year             INT             NULL
          , start_month            INT             NULL
          , start_day              INT             NULL
          , end_year               INT             NULL
          , end_month              INT             NULL
          , end_day                INT             NULL
          , details                VARCHAR         NULL
        );
        CREATE INDEX object_dates_sd_oid ON {schema}.object_dates(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_instances(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_instances;
        CREATE TABLE {schema}.object_instances(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , repository_org_id      INT             NULL
          , repository_org         VARCHAR         NULL
          , url                    VARCHAR         NULL
          , url_accessible         BOOLEAN         NULL
          , url_last_checked       DATE            NULL
          , resource_type_id       INT             NULL
          , resource_type          VARCHAR         NULL
          , resource_size          VARCHAR         NULL
          , resource_size_units    VARCHAR         NULL
          , resource_comments      VARCHAR         NULL
        );
        CREATE INDEX object_instances_sd_oid ON {schema}.object_instances(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_people(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_people;
        CREATE TABLE {schema}.object_people(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
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
        CREATE INDEX object_people_sd_oid ON {schema}.object_people(sd_oid);";

        Execute_SQL(sql_string);
    }
    
    
    public void create_table_object_organisations(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_organisations;
        CREATE TABLE {schema}.object_organisations(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , contrib_type_id        INT             NULL
          , contrib_type           VARCHAR         NULL
          , organisation_id        INT             NULL
          , organisation_name      VARCHAR         NULL
        );
        CREATE INDEX object_organisations_sd_oid ON {schema}.object_organisations(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_titles(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_titles;
        CREATE TABLE {schema}.object_titles(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , title_type_id          INT             NULL
          , title_type             VARCHAR         NULL
          , title_text             VARCHAR         NULL
          , lang_code              VARCHAR         NULL
          , lang_usage_id          INT             NOT NULL default 11
          , is_default             BOOLEAN         NULL
          , comments               VARCHAR         NULL
        );
        CREATE INDEX object_titles_sd_oid ON {schema}.object_titles(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_topics(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_topics;
        CREATE TABLE {schema}.object_topics(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , topic_type_id          INT             NULL
          , topic_type             VARCHAR         NULL
          , original_value         VARCHAR         NULL    
          , original_ct_type_id    INT             NULL
          , original_ct_type       VARCHAR         NULL
          , original_ct_code       VARCHAR         NULL          
          , mesh_code              VARCHAR         NULL
          , mesh_value             VARCHAR         NULL
        );
        CREATE INDEX object_topics_sd_oid ON {schema}.object_topics(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_comments(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_comments;
        CREATE TABLE {schema}.object_comments(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , ref_type               VARCHAR         NULL 
          , ref_source             VARCHAR         NULL 
          , pmid                   VARCHAR         NULL 
          , pmid_version           VARCHAR         NULL 
          , notes                  VARCHAR         NULL 
        );
        CREATE INDEX object_comments_sd_oid ON {schema}.object_comments(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_descriptions(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_descriptions;
        CREATE TABLE {schema}.object_descriptions(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR        NOT NULL
          , description_type_id    INT             NULL
          , description_type       VARCHAR         NULL
          , label                  VARCHAR         NULL
          , description_text       VARCHAR         NULL
          , lang_code              VARCHAR         NULL
        );
        CREATE INDEX object_descriptions_sd_oid ON {schema}.object_descriptions(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_identifiers(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_identifiers;
        CREATE TABLE {schema}.object_identifiers(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , identifier_value       VARCHAR         NULL
          , identifier_type_id     INT             NULL
          , identifier_type        VARCHAR         NULL
          , identifier_org_id      INT             NULL
          , identifier_org         VARCHAR         NULL
          , identifier_date        VARCHAR         NULL
        );
        CREATE INDEX object_identifiers_sd_oid ON {schema}.object_identifiers(sd_oid);";

        Execute_SQL(sql_string);
    }

    public void create_table_object_db_links(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_db_links;
        CREATE TABLE {schema}.object_db_links(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , db_sequence            INT             NULL
          , db_name                VARCHAR         NULL
          , id_in_db               VARCHAR         NULL
        );
        CREATE INDEX object_db_links_sd_oid ON {schema}.object_db_links(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_publication_types(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_publication_types;
        CREATE TABLE {schema}.object_publication_types(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , type_name              VARCHAR         NULL
        );
        CREATE INDEX object_publication_types_sd_oid ON {schema}.object_publication_types(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_relationships(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_relationships;
        CREATE TABLE {schema}.object_relationships(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , relationship_type_id   INT             NULL
          , relationship_type      VARCHAR         NULL
          , target_sd_oid          VARCHAR         NULL
        );
        CREATE INDEX object_relationships_sd_oid ON {schema}.object_relationships(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_rights(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.object_rights;
        CREATE TABLE {schema}.object_rights(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , rights_name            VARCHAR         NULL
          , rights_uri             VARCHAR         NULL
          , comments               VARCHAR         NULL
        );
        CREATE INDEX object_rights_sd_oid ON {schema}.object_rights(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_journal_details(string schema)
    {
        string sql_string = $@"DROP TABLE IF EXISTS {schema}.journal_details;
        CREATE TABLE {schema}.journal_details(
            id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
          , sd_oid                 VARCHAR         NOT NULL
          , pissn                  VARCHAR         NULL
          , eissn                  VARCHAR         NULL
          , journal_title          VARCHAR         NULL
        );
        CREATE INDEX journal_details_sd_oid ON {schema}.journal_details(sd_oid);";

        Execute_SQL(sql_string);
    }
}