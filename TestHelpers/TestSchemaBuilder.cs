using Dapper;
using Npgsql;


namespace MDR_Harvester;

public class TestSchemaBuilder
{
    string _db_conn;

    public TestSchemaBuilder(string db_conn)
    {
        _db_conn = db_conn;
    }


    public void Execute_SQL(string sql_string)
    {
        using (var conn = new NpgsqlConnection(_db_conn))
        {
            conn.Execute(sql_string);
        }
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
        ExpectedStudyTableBuilder studytablebuilder = new ExpectedStudyTableBuilder(_db_conn);

        studytablebuilder.create_table_studies();
        studytablebuilder.create_table_study_identifiers();
        studytablebuilder.create_table_study_titles();
        studytablebuilder.create_table_study_features();
        studytablebuilder.create_table_study_topics();
        studytablebuilder.create_table_study_contributors();
        studytablebuilder.create_table_study_references();
        studytablebuilder.create_table_study_relationships();
        studytablebuilder.create_table_ipd_available();
        studytablebuilder.create_table_study_links();

        ExpectedObjectTableBuilder objecttablebuilder = new ExpectedObjectTableBuilder(_db_conn);

        objecttablebuilder.create_table_data_objects();
        objecttablebuilder.create_table_object_datasets();
        objecttablebuilder.create_table_object_dates();
        objecttablebuilder.create_table_object_instances();
        objecttablebuilder.create_table_object_contributors();
        objecttablebuilder.create_table_object_titles();
        objecttablebuilder.create_table_object_topics();
        objecttablebuilder.create_table_object_descriptions();
        objecttablebuilder.create_table_object_identifiers();
        objecttablebuilder.create_table_object_db_links();
        objecttablebuilder.create_table_object_publication_types();
        objecttablebuilder.create_table_object_rights();
        objecttablebuilder.create_table_object_comments();
        objecttablebuilder.create_table_object_relationships();
        objecttablebuilder.create_table_journal_details();

    }


    public void SetUpSDCompositeTables()
    {
        SDCompStudyTableBuilder studytablebuilder = new SDCompStudyTableBuilder(_db_conn);

        studytablebuilder.create_table_studies();
        studytablebuilder.create_table_study_identifiers();
        studytablebuilder.create_table_study_titles();
        studytablebuilder.create_table_study_features();
        studytablebuilder.create_table_study_topics();
        studytablebuilder.create_table_study_contributors();
        studytablebuilder.create_table_study_references();
        studytablebuilder.create_table_study_relationships();
        studytablebuilder.create_table_ipd_available();
        studytablebuilder.create_table_study_links();
        studytablebuilder.create_table_study_hashes();

        SDCompObjectTableBuilder objecttablebuilder = new SDCompObjectTableBuilder(_db_conn);

        objecttablebuilder.create_table_data_objects();
        objecttablebuilder.create_table_object_datasets();
        objecttablebuilder.create_table_object_dates();
        objecttablebuilder.create_table_object_instances();
        objecttablebuilder.create_table_object_contributors();
        objecttablebuilder.create_table_object_titles();
        objecttablebuilder.create_table_object_topics();
        objecttablebuilder.create_table_object_descriptions();
        objecttablebuilder.create_table_object_identifiers();
        objecttablebuilder.create_table_object_db_links();
        objecttablebuilder.create_table_object_publication_types();
        objecttablebuilder.create_table_object_rights();
        objecttablebuilder.create_table_object_comments();
        objecttablebuilder.create_table_object_relationships();
        objecttablebuilder.create_table_object_hashes();
    }


    public void TearDownForeignSchema()
    {
        using (var conn = new NpgsqlConnection(_db_conn))
        {
            string sql_string = @"DROP USER MAPPING IF EXISTS FOR CURRENT_USER
                     SERVER mon;";
            conn.Execute(sql_string);

            sql_string = @"DROP SERVER IF EXISTS mon CASCADE;";
            conn.Execute(sql_string);

            sql_string = @"DROP SCHEMA IF EXISTS mon_sf cascade;";
            conn.Execute(sql_string);


        }

    }
}

