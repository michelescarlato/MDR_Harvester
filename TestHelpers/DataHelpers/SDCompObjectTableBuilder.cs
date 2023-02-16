using Dapper;
using Npgsql;

namespace MDR_Harvester;

public class SDCompObjectTableBuilder
{
    private readonly string _db_conn;

    public SDCompObjectTableBuilder(string db_conn)
    {
        _db_conn = db_conn;
    }

    private void Execute_SQL(string sql_string)
    {
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }

    public void create_table_data_objects()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.data_objects;
              CREATE TABLE sdcomp.data_objects(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL       
              , sd_oid                 VARCHAR         NOT NULL
              , sd_sid                 VARCHAR         NULL
              , display_title          VARCHAR         NULL
              , version                VARCHAR         NULL
              , doi                    VARCHAR         NULL 
              , doi_status_id          INT             NULL
              , publication_year       INT             NULL
              , object_class_id        INT             NULL
              , object_class           VARCHAR         NULL
              , object_type_id         INT             NULL
              , object_type            VARCHAR         NULL
              , managing_org_id        INT             NULL
              , managing_org           VARCHAR         NULL
              , managing_org_ror_id    VARCHAR         NULL
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
              , record_hash            CHAR(32)        NULL
              , object_full_hash       CHAR(32)        NULL
            );
            CREATE INDEX data_objects_sd_oid ON sdcomp.data_objects(sd_oid);
            CREATE INDEX data_objects_sd_sid ON sdcomp.data_objects(sd_sid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_datasets()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_datasets;
            CREATE TABLE sdcomp.object_datasets(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
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
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_datasets_sd_oid ON sdcomp.object_datasets(sd_oid);";

        Execute_SQL(sql_string);
    }
			

    public void create_table_object_dates()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_dates;
            CREATE TABLE sdcomp.object_dates(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
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
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_dates_sd_oid ON sdcomp.object_dates(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_instances()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_instances;
            CREATE TABLE sdcomp.object_instances(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , instance_type_id       INT             NOT NULL 
              , instance_type          VARCHAR         NULL
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
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_instances_sd_oid ON sdcomp.object_instances(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_contributors()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_contributors;
            CREATE TABLE sdcomp.object_contributors(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , contrib_type_id        INT             NULL
              , contrib_type           VARCHAR         NULL
              , is_individual          BOOLEAN         NULL
              , person_id              INT             NULL
              , person_given_name      VARCHAR         NULL
              , person_family_name     VARCHAR         NULL
              , person_full_name       VARCHAR         NULL
              , orcid_id               VARCHAR         NULL
              , person_affiliation     VARCHAR         NULL
              , organisation_id        INT             NULL
              , organisation_name      VARCHAR         NULL
              , organisation_ror_id    VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_contributors_sd_oid ON sdcomp.object_contributors(sd_oid);";

        Execute_SQL(sql_string);
    }

   
    public void create_table_object_titles()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_titles;
            CREATE TABLE sdcomp.object_titles(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , title_type_id          INT             NULL
              , title_type             VARCHAR         NULL
              , title_text             VARCHAR         NULL
              , lang_code              VARCHAR         NULL
              , lang_usage_id          INT             NOT NULL default 11
              , is_default             BOOLEAN         NULL
              , comments               VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_titles_sd_oid ON sdcomp.object_titles(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_topics()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_topics;
            CREATE TABLE sdcomp.object_topics(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , topic_type_id          INT             NULL
              , topic_type             VARCHAR         NULL
              , mesh_coded             BOOLEAN         NULL
              , mesh_code              VARCHAR         NULL
              , mesh_value             VARCHAR         NULL
              , original_ct_id         INT             NULL
              , original_ct_code       VARCHAR         NULL
              , original_value         VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_topics_sd_oid ON sdcomp.object_topics(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_comments()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_comments;
            CREATE TABLE sdcomp.object_comments(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , ref_type               VARCHAR         NULL 
              , ref_source             VARCHAR         NULL 
              , pmid                   VARCHAR         NULL 
              , pmid_version           VARCHAR         NULL 
              , notes                  VARCHAR         NULL 
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_comments_sd_oid ON sdcomp.object_comments(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_descriptions()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_descriptions;
            CREATE TABLE sdcomp.object_descriptions(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , description_type_id    INT             NULL
              , description_type       VARCHAR         NULL
              , label                  VARCHAR         NULL
              , description_text       VARCHAR         NULL
              , lang_code              VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_descriptions_sd_oid ON sdcomp.object_descriptions(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_identifiers()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_identifiers;
            CREATE TABLE sdcomp.object_identifiers(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , identifier_value       VARCHAR         NULL
              , identifier_type_id     INT             NULL
              , identifier_type        VARCHAR         NULL
              , identifier_org_id      INT             NULL
              , identifier_org         VARCHAR         NULL
              , identifier_org_ror_id  VARCHAR         NULL
              , identifier_date        VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_identifiers_sd_oid ON sdcomp.object_identifiers(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_db_links()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_db_links;
            CREATE TABLE sdcomp.object_db_links(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , db_sequence            INT             NULL
              , db_name                VARCHAR         NULL
              , id_in_db               VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_db_links_sd_oid ON sdcomp.object_db_links(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_publication_types()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_publication_types;
            CREATE TABLE sdcomp.object_publication_types(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , type_name              VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_publication_types_sd_oid ON sdcomp.object_publication_types(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_relationships()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_relationships;
            CREATE TABLE sdcomp.object_relationships(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , relationship_type_id   INT             NULL
              , relationship_type      VARCHAR         NULL
              , target_sd_oid          VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_relationships_sd_oid ON sdcomp.object_relationships(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_rights()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_rights;
            CREATE TABLE sdcomp.object_rights(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , rights_name            VARCHAR         NULL
              , rights_uri             VARCHAR         NULL
              , comments               VARCHAR         NULL
              , record_hash            CHAR(32)        NULL
            );
            CREATE INDEX object_rights_sd_oid ON sdcomp.object_rights(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_citation_objects()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.citation_objects;
            CREATE TABLE sdcomp.citation_objects(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , sd_sid                 VARCHAR         NULL
              , display_title          VARCHAR         NULL
              , version                VARCHAR         NULL
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
              , journal_title          VARCHAR         NULL
              , pissn                  VARCHAR         NULL
              , eissn                  VARCHAR         NULL
            );
            CREATE INDEX citation_objects_sd_oid ON sdcomp.citation_objects(sd_oid);";

        Execute_SQL(sql_string);
    }


    public void create_table_object_hashes()
    {
        string sql_string = @"DROP TABLE IF EXISTS sdcomp.object_hashes;
            CREATE TABLE sdcomp.object_hashes(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , source_id              INT             NOT NULL
              , sd_oid                 VARCHAR         NOT NULL
              , hash_type_id           INT             NULL
              , hash_type              VARCHAR         NULL
              , composite_hash         CHAR(32)        NULL
            );
            CREATE INDEX object_hashes_sd_oid ON sdcomp.object_hashes(sd_oid);";

        Execute_SQL(sql_string);
    }

}