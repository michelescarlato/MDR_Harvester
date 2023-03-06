using Dapper;
using Npgsql;

namespace MDR_Harvester;

public class TestSchemaBuilder
{
    private readonly string _db_conn;

    public TestSchemaBuilder(string db_conn)
    {
        _db_conn = db_conn;
    }

    private void Execute_SQL(string sql_string)
    {
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }

    public void SetUpMonSchema()
    {
        string sql_string = @"CREATE EXTENSION IF NOT EXISTS postgres_fdw schema sd;";
        Execute_SQL(sql_string);

        sql_string = @"CREATE SERVER IF NOT EXISTS mon
                   FOREIGN DATA WRAPPER postgres_fdw
                   OPTIONS(host 'localhost', dbname 'mon')";
        Execute_SQL(sql_string);

        sql_string = @"CREATE USER MAPPING IF NOT EXISTS FOR CURRENT_USER
                   SERVER mon
                   OPTIONS(user 'postgres', password 'WinterIsComing!')";
        Execute_SQL(sql_string);

        sql_string = @"DROP SCHEMA IF EXISTS mon_sf cascade;
                   CREATE SCHEMA mon_sf;
                   IMPORT FOREIGN SCHEMA sf
                   FROM SERVER mon
                   INTO mon_sf;";
        Execute_SQL(sql_string);
    }


    public void SetUpExpectedTables()
    {
        StudyTableBuilder study_table_builder = new StudyTableBuilder(_db_conn);
        ObjectTableBuilder object_table_builder = new ObjectTableBuilder(_db_conn);
        
        study_table_builder.create_table_studies("expected");
        study_table_builder.create_table_study_identifiers("expected");
        study_table_builder.create_table_study_titles("expected");
        study_table_builder.create_table_study_features("expected");
        study_table_builder.create_table_study_topics("expected");
        study_table_builder.create_table_study_people("expected");
        study_table_builder.create_table_study_organisations("expected");
        study_table_builder.create_table_study_references("expected");
        study_table_builder.create_table_study_relationships("expected");
        study_table_builder.create_table_ipd_available("expected");
        study_table_builder.create_table_study_links("expected");
        study_table_builder.create_table_study_countries("expected");
        study_table_builder.create_table_study_locations("expected");
        study_table_builder.create_table_study_conditions("expected");
        study_table_builder.create_table_study_iec("expected");

        object_table_builder.create_table_data_objects("expected");
        object_table_builder.create_table_object_datasets("expected");
        object_table_builder.create_table_object_dates("expected");
        object_table_builder.create_table_object_instances("expected");
        object_table_builder.create_table_object_people("expected");
        object_table_builder.create_table_object_organisations("expected");
        object_table_builder.create_table_object_titles("expected");
        object_table_builder.create_table_object_topics("expected");
        object_table_builder.create_table_object_descriptions("expected");
        object_table_builder.create_table_object_identifiers("expected");
        object_table_builder.create_table_object_db_links("expected");
        object_table_builder.create_table_object_publication_types("expected");
        object_table_builder.create_table_object_rights("expected");
        object_table_builder.create_table_object_comments("expected");
        object_table_builder.create_table_object_relationships("expected");
        object_table_builder.create_table_journal_details("expected");
    }


    public void SetUpSDCompositeTables()
    {
        StudyTableBuilder study_table_builder = new StudyTableBuilder(_db_conn);
        ObjectTableBuilder object_table_builder = new ObjectTableBuilder(_db_conn);
        
        study_table_builder.create_table_studies("sdcomp");
        study_table_builder.create_table_study_identifiers("sdcomp");
        study_table_builder.create_table_study_titles("sdcomp");
        study_table_builder.create_table_study_features("sdcomp");
        study_table_builder.create_table_study_topics("sdcomp");
        study_table_builder.create_table_study_people("sdcomp");
        study_table_builder.create_table_study_organisations("sdcomp");
        study_table_builder.create_table_study_references("sdcomp");
        study_table_builder.create_table_study_relationships("sdcomp");
        study_table_builder.create_table_ipd_available("sdcomp");
        study_table_builder.create_table_study_links("sdcomp");
        study_table_builder.create_table_study_countries("sdcomp");
        study_table_builder.create_table_study_locations("sdcomp");
        study_table_builder.create_table_study_conditions("sdcomp");
        study_table_builder.create_table_study_iec("sdcomp");

        object_table_builder.create_table_data_objects("sdcomp");
        object_table_builder.create_table_object_datasets("sdcomp");
        object_table_builder.create_table_object_dates("sdcomp");
        object_table_builder.create_table_object_instances("sdcomp");
        object_table_builder.create_table_object_people("sdcomp");
        object_table_builder.create_table_object_organisations("sdcomp");
        object_table_builder.create_table_object_titles("sdcomp");
        object_table_builder.create_table_object_topics("sdcomp");
        object_table_builder.create_table_object_descriptions("sdcomp");
        object_table_builder.create_table_object_identifiers("sdcomp");
        object_table_builder.create_table_object_db_links("sdcomp");
        object_table_builder.create_table_object_publication_types("sdcomp");
        object_table_builder.create_table_object_rights("sdcomp");
        object_table_builder.create_table_object_comments("sdcomp");
        object_table_builder.create_table_object_relationships("sdcomp");
        object_table_builder.create_table_journal_details("sdcomp");
    }


    public void TearDownForeignSchema()
    {
        using var conn = new NpgsqlConnection(_db_conn);
        string sql_string = @"DROP USER MAPPING IF EXISTS FOR CURRENT_USER
                     SERVER mon;";
        conn.Execute(sql_string);

        sql_string = @"DROP SERVER IF EXISTS mon CASCADE;";
        conn.Execute(sql_string);

        sql_string = @"DROP SCHEMA IF EXISTS mon_sf cascade;";
        conn.Execute(sql_string);
    }
}

