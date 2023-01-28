using Dapper;
using Npgsql;

namespace MDR_Harvester;

public class TestingDataLayer : ITestingDataLayer
{
    ICredentials _credentials;
    NpgsqlConnectionStringBuilder builder;
    private string _db_conn;
    LoggingHelper _loggingHelper;

    /// <summary>
    /// Constructor is used to build the connection string, 
    /// using a credentials object that has the relevant credentials 
    /// from the app settings, themselves derived from a json file.
    /// </summary>
    /// 
    public TestingDataLayer(ICredentials credentials)
    {
        builder = new NpgsqlConnectionStringBuilder();

        builder.Host = credentials.Host;
        builder.Username = credentials.Username;
        builder.Password = credentials.Password;

        builder.Database = "test";
        _db_conn = builder.ConnectionString;

        _credentials = credentials;

        
    }

    public Credentials Credentials => (Credentials)_credentials;


    public int EstablishExpectedData()
    {
        _loggingHelper.OpenLogFile("test");

        try
        {
            _loggingHelper.LogLine("STARTING EXPECTED DATA ASSEMBLY");

            TestSchemaBuilder tsb = new TestSchemaBuilder(_db_conn);

            tsb.SetUpMonSchema();
            _loggingHelper.LogLine("mon_sf link established");

            tsb.SetUpExpectedTables();
            _loggingHelper.LogLine("Expected Data tables recreated");

            tsb.SetUpSDCompositeTables();
            _loggingHelper.LogLine("SD composite test data tables recreated");

            ExpectedDataBuilder edb = new ExpectedDataBuilder(_db_conn);

            edb.InitialiseTestStudiesList();
            edb.InitialiseTestPubMedObjectsList();
            _loggingHelper.LogLine("List of test studies and pubmed objects inserted");

            edb.LoadInitialInputTables();
            _loggingHelper.LogLine("Data loaded from manual inspections");  

            //edb.CalculateAndAddOIDs();
            //_loggingHelper.Information("OIDs calculated and inserted");

            tsb.TearDownForeignSchema();
            _loggingHelper.LogLine("mon_sf link deleted");

            return 0;
        }

        catch (Exception e)
        {
            _loggingHelper.LogCodeError("Error in establishing test data from stored procedures", e.Message, e.StackTrace);
            _loggingHelper.LogLine("Closing Log");
            _loggingHelper.CloseLog();
            return -1;
        }
    }

    public void TransferTestSDData(ISource source)
    {
        TransferSDDataBuilder tdb = new TransferSDDataBuilder(source);

        if (source.has_study_tables == true)
        {
            tdb.DeleteExistingStudyData();
            tdb.TransferStudyData();  
            _loggingHelper.LogLine("New study SD test data for source " + source.id + " added to CompSD");
        }

        tdb.DeleteExistingObjectData();
        tdb.TransferObjectData();
        _loggingHelper.LogLine("New object SD test data for source " + source.id + " added to CompSD");
    }


    public IEnumerable<int> ObtainTestSourceIDs()
    {
        string sql_string = @"select distinct source_id 
                                 from expected.source_studies
                                 union
                                 select distinct source_id 
                                 from expected.source_objects;";

        using (var conn = new NpgsqlConnection(_db_conn))
        {
            return conn.Query<int>(sql_string);
        }
    }

}
