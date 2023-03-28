using Dapper;
using Dapper.Contrib.Extensions;
using Npgsql;

namespace MDR_Harvester;

public class MonDataLayer : IMonDataLayer
{
    private readonly ILoggingHelper _logging_helper;
    private readonly ICredentials _credentials;
    private readonly string connString;
    
    public MonDataLayer(ILoggingHelper logging_helper, ICredentials credentials)
    {
        _logging_helper = logging_helper; 
        _credentials = credentials;
        connString = _credentials.GetConnectionString("mon", 1);
    }
    

    public string GetConnectionString(string database_name, int harvest_type_id)
    {
        return _credentials.GetConnectionString(database_name, harvest_type_id);
    }

    public bool SourceIdPresent(int source_id)
    {
        string sql_string = $"Select id from sf.source_parameters where id = {source_id}";
        using NpgsqlConnection Conn = new(connString);
        int res = Conn.QueryFirstOrDefault<int>(sql_string);
        return (res != 0);
    }
     

    public Source FetchSourceParameters(int source_id)
    {
        using NpgsqlConnection Conn = new(connString);
        return Conn.Get<Source>(source_id);
    }


    public int GetNextHarvestEventId()
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = "select max(id) from sf.harvest_events ";
        int last_id = Conn.ExecuteScalar<int>(sql_string);
        return last_id + 1;
    }

    public int FetchFileRecordsCount(int source_id, string source_type,
                                   int harvest_type_id = 1, int days_ago = 0)
    {
        string sql_string = "select count(*) ";
        sql_string += source_type.ToLower() == "study" ? "from sf.source_data_studies"
                                             : "from sf.source_data_objects";
        sql_string += GetWhereClause(source_id, harvest_type_id, days_ago);

        using NpgsqlConnection Conn = new(connString);
        return Conn.ExecuteScalar<int>(sql_string);
    }


    public int FetchFullFileCount(int source_id, string source_type, int harvest_type_id)
    {
        string sql_string = "select count(*) ";
        sql_string += source_type.ToLower() == "study" ? "from sf.source_data_studies"
                                             : "from sf.source_data_objects";
        sql_string += " where source_id = " + source_id;
        sql_string += " and local_path is not null";
        
        if (harvest_type_id == 3)
        {
            sql_string += " and for_testing = true";
        }
        using NpgsqlConnection Conn = new(connString);
        return Conn.ExecuteScalar<int>(sql_string);
    }


    public IEnumerable<StudyFileRecord> FetchStudyFileRecordsByOffset(int source_id, int offset_num,
                                  int amount, int harvest_type_id = 1, int days_ago = 0)
    {
        string sql_string = GetRecordSelectList();
        sql_string += " from sf.source_data_studies ";
        sql_string += GetWhereClause(source_id, harvest_type_id, days_ago);
        sql_string += " order by local_path ";
        if (harvest_type_id == 4)
        {
            sql_string += " limit " + amount;  // offset does not work here as the pool of valid records decreases
        }
        else
        {
            sql_string += " offset " + offset_num + " limit " + amount;
        }

        using NpgsqlConnection Conn = new(connString);
        return Conn.Query<StudyFileRecord>(sql_string);
    }

    public IEnumerable<ObjectFileRecord> FetchObjectFileRecordsByOffset(int source_id, int offset_num,
                                 int amount, int harvest_type_id = 1, int days_ago = 0)
    {
        string sql_string = GetRecordSelectList();
        sql_string += " from sf.source_data_objects ";
        sql_string += GetWhereClause(source_id, harvest_type_id, days_ago);
        sql_string += " order by local_path ";
        sql_string += " offset " + offset_num + " limit " + amount;

        using NpgsqlConnection Conn = new(connString);
        return Conn.Query<ObjectFileRecord>(sql_string);
    }

    private string GetWhereClause(int source_id, int harvest_type_id, int days_ago)
    {
        string where_clause = "";
        if (harvest_type_id == 1)
        {
            // Count all files.
            where_clause = $" where source_id = {source_id} ";
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

            where_clause = @$" where source_id  = {source_id} 
                           and (last_downloaded >= last_imported or last_imported is null) ";
        }
        else if (harvest_type_id == 3)
        {
            // use records marked for testing only
            where_clause = @$" where source_id = {source_id} 
                           and for_testing = true ";
        }
        else if (harvest_type_id == 4)
        {
            // use records not harvested recently (rather than download / import dates) - use for harvest repair
            where_clause = @$" where source_id = {source_id} 
                               and (last_harvested::date < now()::date - {days_ago} or last_harvested is null) ";
        }
        where_clause += " and local_path is not null";
        return where_clause;
    }


    private string GetRecordSelectList()
    {
        string sql_file_select_string = "select id, source_id, sd_id, remote_url, last_revised, ";
        sql_file_select_string += " assume_complete, download_status, local_path, last_saf_id, last_downloaded, ";
        sql_file_select_string += " last_harvest_id, last_harvested, last_import_id, last_imported ";
        return sql_file_select_string;
    }

    
    public void UpdateFileRecLastHarvested(int? id, string source_type, int last_harvest_id)
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = source_type.ToLower() == "study" ? "update sf.source_data_studies"
                                                       : "update sf.source_data_objects";
        sql_string += " set last_harvest_id = " + last_harvest_id.ToString() + ", ";
        sql_string += " last_harvested = current_timestamp";
        sql_string += " where id = " + id.ToString();
        Conn.Execute(sql_string);
    }

    public int StoreHarvestEvent(HarvestEvent harvest)
    {
        using NpgsqlConnection Conn = new(connString);
        return (int)Conn.Insert(harvest);
    }
}

