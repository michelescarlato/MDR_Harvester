using Dapper;
using Npgsql;


namespace MDR_Harvester
{
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

        public void create_table_studies()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.studies;
            CREATE TABLE sd.studies(
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
            CREATE INDEX studies_sid ON sd.studies(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_identifiers()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_identifiers;
            CREATE TABLE sd.study_identifiers(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , identifier_value       VARCHAR         NULL
              , identifier_type_id     INT             NULL
              , identifier_type        VARCHAR         NULL
              , identifier_org_id      INT             NULL
              , identifier_org         VARCHAR         NULL
              , identifier_org_ror_id  VARCHAR         NULL
              , identifier_date        VARCHAR         NULL
              , identifier_link        VARCHAR         NULL
            );
            CREATE INDEX study_identifiers_sd_sid ON sd.study_identifiers(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_relationships()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_relationships;
            CREATE TABLE sd.study_relationships(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , relationship_type_id   INT             NULL
              , relationship_type      VARCHAR         NULL
              , target_sd_sid          VARCHAR         NULL
            );
            CREATE INDEX study_relationships_sd_sid ON sd.study_relationships(sd_sid);
            CREATE INDEX study_relationships_target_sd_sid ON sd.study_relationships(target_sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_references()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_references;
            CREATE TABLE sd.study_references(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , citation               VARCHAR         NULL
              , pmid                   VARCHAR         NULL
              , doi                    VARCHAR         NULL	
              , type_id                INT             NULL
              , type                   VARCHAR         NULL
              , comments               VARCHAR         NULL
            );
            CREATE INDEX study_references_sd_sid ON sd.study_references(sd_sid);";

            Execute_SQL(sql_string);
        }

        public void create_table_study_titles()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_titles;
            CREATE TABLE sd.study_titles(
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
            CREATE INDEX study_titles_sd_sid ON sd.study_titles(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_contributors()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_contributors;
            CREATE TABLE sd.study_contributors(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
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
            );
            CREATE INDEX study_contributors_sd_sid ON sd.study_contributors(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_topics()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_topics;
            CREATE TABLE sd.study_topics(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , topic_type_id          INT             NULL
              , topic_type             VARCHAR         NULL
              , mesh_coded             BOOLEAN         NULL
              , mesh_code              VARCHAR         NULL
              , mesh_value             VARCHAR         NULL
              , original_ct_id         INT             NULL
              , original_ct_code       VARCHAR         NULL
              , original_value         VARCHAR         NULL
            );
            CREATE INDEX study_topics_sd_sid ON sd.study_topics(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_conditions()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_conditions;
            CREATE TABLE sd.study_conditions(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , original_value         VARCHAR         NULL
              , original_ct            INT             NULL
              , original_ct_code       VARCHAR         NULL                 
              , icd_code               VARCHAR         NULL
              , icd_name               VARCHAR         NULL
            );
            CREATE INDEX study_conditions_sd_sid ON sd.study_conditions(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_features()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_features;
            CREATE TABLE sd.study_features(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , feature_type_id        INT             NULL
              , feature_type           VARCHAR         NULL
              , feature_value_id       INT             NULL
              , feature_value          VARCHAR         NULL
            );
            CREATE INDEX study_features_sid ON sd.study_features(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_iec()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_iec;
            CREATE TABLE sd.study_iec(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , seq_num                INT             NULL
              , leader                 VARCHAR         NOT NULL
              , indent_level           INT             NULL
              , level_seq_num          INT             NULL
              , iec_type_id            INT             NULL
              , iec_type               VARCHAR         NULL
              , iec_text               VARCHAR         NULL
              , iec_class_id           INT             NULL
              , iec_class              VARCHAR         NULL
              , iec_parsed_text        VARCHAR         NULL
            );
            CREATE INDEX study_iec_sid ON sd.study_iec(sd_sid);";

            Execute_SQL(sql_string);
        }

        public void create_table_study_links()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_links;
            CREATE TABLE sd.study_links(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , link_label             VARCHAR         NULL
              , link_url               VARCHAR         NULL
            );
            CREATE INDEX study_links_sd_sid ON sd.study_links(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_locations()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_locations;
            CREATE TABLE sd.study_locations(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , facility_org_id        INT             NULL
              , facility               VARCHAR         NULL
              , facility_ror_id        VARCHAR         NULL
              , city_id                INT             NULL
              , city_name              VARCHAR         NULL
              , country_id             INT             NULL
              , country_name           VARCHAR         NULL
              , status_id              INT             NULL
              , status                 VARCHAR         NULL
            );
            CREATE INDEX study_locations_sd_sid ON sd.study_locations(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_study_countries()
        {  
            string sql_string = @"DROP TABLE IF EXISTS sd.study_countries;
            CREATE TABLE sd.study_countries(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , country_id             INT             NULL
              , country_name           VARCHAR         NULL
              , status_id              INT             NULL
              , status                 VARCHAR         NULL
            );
            CREATE INDEX study_countries_sd_sid ON sd.study_countries(sd_sid);";

            Execute_SQL(sql_string);
        }


        public void create_table_ipd_available()
        {
            string sql_string = @"DROP TABLE IF EXISTS sd.study_ipd_available;
            CREATE TABLE sd.study_ipd_available(
                id                     INT             GENERATED ALWAYS AS IDENTITY PRIMARY KEY
              , sd_sid                 VARCHAR         NOT NULL
              , ipd_id                 VARCHAR         NULL
              , ipd_type               VARCHAR         NULL
              , ipd_url                VARCHAR         NULL
              , ipd_comment            VARCHAR         NULL
            );
            CREATE INDEX study_ipd_available_sd_sid ON sd.study_ipd_available(sd_sid);";

            Execute_SQL(sql_string);
        }
    }
}
