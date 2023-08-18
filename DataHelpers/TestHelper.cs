using Dapper;
using Npgsql;
namespace MDR_Harvester;

public class TestHelper
{
    private readonly ILoggingHelper _loggingHelper;
    private readonly Source _source;
    private readonly string _db_conn;
    
    public TestHelper(Source source, ILoggingHelper loggingHelper)
    {
        _source = source;
        _loggingHelper = loggingHelper;
        _db_conn = source.db_conn!;
    }

    public int EstablishTempStudyTestList()
    {
        using var conn = new NpgsqlConnection(_db_conn);
        string sql_string = @"DROP TABLE IF EXISTS mn.test_study_list;
                        CREATE TABLE mn.test_study_list as 
                        SELECT sd.sd_sid from mn.source_data sd
                        WHERE sd.for_testing = true";
        conn.Execute(sql_string);

        sql_string = @"select count(*) from mn.test_study_list";
        int res = conn.ExecuteScalar<int>(sql_string);
        if (res > 0)
        {
            sql_string = @"DROP TABLE IF EXISTS mn.test_object_list;
                                CREATE TABLE mn.test_object_list as 
                                SELECT sd_oid from ad.data_objects sdo
                                inner join mn.test_study_list tsl
                                on sdo.sd_sid = tsl.sd_sid";
            conn.Execute(sql_string);
        }
        return res;
    }
   
    public int EstablishTempObjectTestList()
    {
        using var conn = new NpgsqlConnection(_db_conn);
        string sql_string = @"DROP TABLE IF EXISTS mn.test_object_list;
                        CREATE TABLE mn.test_object_list as 
                        SELECT sd.sd_oid from mn.source_data sd
                        WHERE sd.for_testing = true";
        conn.Execute(sql_string);
        
        sql_string = @"select count(*) from mn.test_object_list";
        return conn.ExecuteScalar<int>(sql_string);
    }
    
    public void TeardownTempTestDataTables()
    {
        string sql_string = @"DROP TABLE IF EXISTS mn.test_study_list;
                            DROP TABLE IF EXISTS mn.test_object_list;";
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }
 
    
    private void delete_study_test_recs(string schema_name, string table_name)
    {
        string sql_string = $@"Delete from {schema_name}.{table_name} t
                               using mn.test_study_list s
                               where t.sd_sid = s.sd_sid";
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }
    
    private void delete_object_test_recs(string schema_name, string table_name)
    {
        string sql_string = $@"Delete from {schema_name}.{table_name} t
                               using mn.test_object_list s
                               where t.sd_oid = s.sd_oid";
        using var conn = new NpgsqlConnection(_db_conn);
        conn.Execute(sql_string);
    }

    public void DeleteCurrentTestStudyData()
    {
        // these common to all databases

        delete_study_test_recs("sd", "studies");
        delete_study_test_recs("sd", "study_titles");
        delete_study_test_recs("sd", "study_identifiers");

        // these are database dependent
        if (_source.has_study_topics is true) delete_study_test_recs("sd", "study_topics");
        if (_source.has_study_conditions is true) delete_study_test_recs("sd", "study_conditions");
        if (_source.has_study_features is true) delete_study_test_recs("sd", "study_features");
        if (_source.has_study_people is true) delete_study_test_recs("sd", "study_people");
        if (_source.has_study_organisations is true) delete_study_test_recs("sd", "study_organisations");
        if (_source.has_study_references is true) delete_study_test_recs("sd", "study_references");
        if (_source.has_study_relationships is true) delete_study_test_recs("sd", "study_relationships");
        if (_source.has_study_links is true) delete_study_test_recs("sd", "study_links");
        if (_source.has_study_countries is true) delete_study_test_recs("sd", "study_countries");
        if (_source.has_study_locations is true) delete_study_test_recs("sd", "study_locations");
        if (_source.has_study_ipd_available is true) delete_study_test_recs("sd", "study_ipd_available");
        if (_source.has_study_iec is true)
        {
            if (_source.study_iec_storage_type == "Single Table")
            {
                delete_study_test_recs("sd", "study_iec");
            }

            if (_source.study_iec_storage_type == "By Year Groupings")
            {
                delete_study_test_recs("sd", "study_iec_upto12");
                delete_study_test_recs("sd", "study_iec_13to19");
                delete_study_test_recs("sd", "study_iec_20on");
            }

            if (_source.study_iec_storage_type == "By Years")
            {
                delete_study_test_recs("sd", "study_iec_null");
                delete_study_test_recs("sd", "study_iec_pre06");
                delete_study_test_recs("sd", "study_iec_0608");
                delete_study_test_recs("sd", "study_iec_0910");
                delete_study_test_recs("sd", "study_iec_1112");
                delete_study_test_recs("sd", "study_iec_1314");
                for (int i = 15; i <= 30; i++)
                {
                    delete_study_test_recs("sd", $"study_iec_{i}");
                }
            }
        }
        _loggingHelper.LogLine("Study test data deleted");
    }

    public void DeleteCurrentTestObjectData()
    {
        // object tables - these common to all databases

        delete_object_test_recs("sd", "data_objects");
        delete_object_test_recs("sd", "object_titles");
        delete_object_test_recs("sd", "object_instances");

        // these are database dependent		

        if (_source.has_object_datasets is true) delete_object_test_recs("sd", "object_datasets");
        if (_source.has_object_dates is true) delete_object_test_recs("sd", "object_dates");
        if (_source.has_object_relationships is true) delete_object_test_recs("sd", "object_relationships");
        if (_source.has_object_rights is true) delete_object_test_recs("sd", "object_rights");
        if (_source.has_object_pubmed_set is true)
        {
            delete_object_test_recs("sd", "object_people");
            delete_object_test_recs("sd", "object_organisations");
            delete_object_test_recs("sd", "object_topics");
            delete_object_test_recs("sd", "object_comments");
            delete_object_test_recs("sd", "object_descriptions");
            delete_object_test_recs("sd", "object_identifiers");
            delete_object_test_recs("sd", "object_db_links");
            delete_object_test_recs("sd", "object_publication_types");
            delete_object_test_recs("sd", "journal_details");
        }
        _loggingHelper.LogLine("Object test data deleted");
    }

}

