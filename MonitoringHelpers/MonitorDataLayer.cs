using Dapper;
using Dapper.Contrib.Extensions;
using Npgsql;

namespace MDR_Harvester;

public class MonDataLayer : IMonDataLayer
{
    private readonly ILoggingHelper _logging_helper;
    private readonly ICredentials _credentials;
    private readonly string monConnString;
    private string thisDBConnString = "";
    
    public MonDataLayer(ILoggingHelper logging_helper, ICredentials credentials)
    {
        _logging_helper = logging_helper; 
        _credentials = credentials;
        monConnString = _credentials.GetConnectionString("mon", 1);
    }

    public string GetConnectionString(string database_name, int harvest_type_id)
    {
        thisDBConnString = _credentials.GetConnectionString(database_name, harvest_type_id);
        return thisDBConnString;
    }

    public bool SourceIdPresent(int source_id)
    {
        string sql_string = $"Select id from sf.source_parameters where id = {source_id}";
        using NpgsqlConnection Conn = new(monConnString);
        int res = Conn.QueryFirstOrDefault<int>(sql_string);
        return (res != 0);
    }
     

    public Source FetchSourceParameters(int source_id)
    {
        using NpgsqlConnection Conn = new(monConnString);
        return Conn.Get<Source>(source_id);
    }


    public int GetNextHarvestEventId()
    {
        using NpgsqlConnection Conn = new(monConnString);
        string sql_string = "select max(id) from sf.harvest_events ";
        int last_id = Conn.ExecuteScalar<int>(sql_string);
        return last_id + 1;
    }

    public int FetchFileRecordsCount(int harvest_type_id = 1, int days_ago = 0)
    {
        string sql_string = "select count(*) from mn.source_data ";
        sql_string += GetWhereClause(harvest_type_id, days_ago);
        using NpgsqlConnection Conn = new(thisDBConnString);
        return Conn.ExecuteScalar<int>(sql_string);
    }


    public int FetchFullFileCount(int harvest_type_id)
    {
        string sql_string = "select count(*) from mn.source_data ";
        sql_string += " where local_path is not null";
        if (harvest_type_id == 3)
        {
            sql_string += " and for_testing = true";
        }
        using NpgsqlConnection Conn = new(thisDBConnString);
        return Conn.ExecuteScalar<int>(sql_string);
    }


    public IEnumerable<StudyFileRecord> FetchStudyFileRecordsByOffset(int offset_num,
                                  int amount, int harvest_type_id = 1, int days_ago = 0)
    {
        string sql_string = @"select id, sd_sid, remote_url, last_revised, 
                         download_status, local_path, last_dl_id, last_downloaded,
                         last_harvest_id, last_harvested, last_import_id, last_imported 
                         from mn.source_data ";
        sql_string += GetWhereClause(harvest_type_id, days_ago);
        sql_string += " order by local_path ";
        if (harvest_type_id == 4)
        {
            sql_string += " limit " + amount;  // offset does not work here as the pool of valid records decreases
        }
        else
        {
            sql_string += " offset " + offset_num + " limit " + amount;
        }
        using NpgsqlConnection Conn = new(thisDBConnString);
        return Conn.Query<StudyFileRecord>(sql_string);
    }

    public IEnumerable<ObjectFileRecord> FetchObjectFileRecordsByOffset(int offset_num,
                                 int amount, int harvest_type_id = 1, int days_ago = 0)
    {
        string sql_string = @"select id, sd_oid, remote_url, last_revised, 
                         download_status, local_path, last_dl_id, last_downloaded,
                         last_harvest_id, last_harvested, last_import_id, last_imported 
                         from mn.source_data ";
        sql_string += GetWhereClause(harvest_type_id, days_ago);
        sql_string += " order by local_path ";
        if (harvest_type_id == 4)
        {
            sql_string += " limit " + amount;  // offset does not work here as the pool of valid records decreases
        }
        else
        {
            sql_string += " offset " + offset_num + " limit " + amount;
        }
        using NpgsqlConnection Conn = new(thisDBConnString);
        return Conn.Query<ObjectFileRecord>(sql_string);
    }

    private string GetWhereClause(int harvest_type_id, int days_ago)
    {
        string where_clause = "";
        if (harvest_type_id == 1)
        {
            // Count all files which are available to the system (should be all of them).
            
            where_clause = $" where local_path is not null" ;
        }
        else if (harvest_type_id == 2)
        {
            // Harvest files that have been downloaded since the last import, 
            // NOTE - not since the last harvest, as multiple harvests may have
            // been carried out. A file should be harvested for import if it 
            // has not yet been imported, or a new download (possible a new version) 
            // has taken place since the import.
            // So files needed where their download date > import date, or they are new
            // and therefore have a null import date

            where_clause = @$" where local_path is not null 
                               and (last_downloaded >= last_imported or last_imported is null) ";
        }
        else if (harvest_type_id == 3)
        {
            // Use records marked for testing only - this will not work for the moment, test framework to be redone !!!
            
            where_clause = @$" where local_path is not null and for_testing = true ";
        }
        else if (harvest_type_id == 4)
        {
            // use records not harvested recently (rather than download / import dates) - use for harvest repair
            where_clause = @$" local_path is not null
                               and (last_harvested::date < now()::date - {days_ago} or last_harvested is null) ";
        }
        return where_clause;
    }
    
    public void UpdateFileRecLastHarvested(int? id, string source_type, int last_harvest_id)
    {
        
        // needs to be datetime.now to be accurate - current timestamp is not !!!
        
        using NpgsqlConnection Conn = new(thisDBConnString);
        string sql_string = "update mn.source_data";
        sql_string += " set last_harvest_id = " + last_harvest_id + ", ";
        sql_string += " last_harvested = current_timestamp";
        sql_string += " where id = " + id;
        Conn.Execute(sql_string); 
    }

    public int StoreHarvestEvent(HarvestEvent harvest)
    {
        using NpgsqlConnection Conn = new(monConnString);
        return (int)Conn.Insert(harvest);
    }
}

