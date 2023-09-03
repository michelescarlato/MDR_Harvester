using Microsoft.Extensions.Configuration;
using Dapper;
using Npgsql;

namespace MDR_Harvester;

public class LoggingHelper : ILoggingHelper
{
    private readonly string _logfileStartOfPath;
    private readonly string _summaryLogfileStartOfPath;
    private string _logfilePath = "";
    private string _summaryLogfilePath = "";
    private StreamWriter? _sw;
    
    public LoggingHelper()
    {
        IConfigurationRoot settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        _logfileStartOfPath = settings["logFilePath"] ?? "";
        _summaryLogfileStartOfPath = settings["summaryFilePath"] ?? "";
    }
    
    // Used to check if a log file with a named source has been created.

    public string LogFilePath => _logfilePath;
    
    public void OpenLogFile(string databaseName)
    {
        string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(":", "").Replace("T", " ");

        string log_folder_path = Path.Combine(_logfileStartOfPath, databaseName);
        if (!Directory.Exists(log_folder_path))
        {
            Directory.CreateDirectory(log_folder_path);
        }
        
        string log_file_name = "HV " + databaseName + " " + dt_string + ".log";
        
        
        _logfilePath = Path.Combine(log_folder_path, log_file_name);
        _summaryLogfilePath = Path.Combine(_summaryLogfileStartOfPath, log_file_name);
        _sw = new StreamWriter(_logfilePath, true, System.Text.Encoding.UTF8);
    }

    
    public void OpenNoSourceLogFile()
    {
        string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(":", "").Replace("T", " ");
        
        string log_file_name = "HV Source not set " + dt_string + ".log";
        _logfilePath = Path.Combine(_logfileStartOfPath, log_file_name);
        _summaryLogfilePath = Path.Combine(_summaryLogfileStartOfPath, log_file_name);
        _sw = new StreamWriter(_logfilePath, true, System.Text.Encoding.UTF8);
    }

    
    public void LogCommandLineParameters(Options opts)
    {
        int[] source_ids = opts.source_ids!.ToArray();
        if (source_ids.Length == 1)
        {
            LogLine("Source_id is " + source_ids[0].ToString());
        }
        else
        {
            LogLine("Source_ids are " + string.Join(",", source_ids));
        }
        LogLine("Type_id is " + opts.harvest_type_id.ToString());
        LogLine("");
    }


    public void LogLine(string message, string identifier = "")
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string feedback = dt_prefix + message + identifier;
        Transmit(feedback);
    }

    public void LogHeader(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string header = dt_prefix + "**** " + message.ToUpper().ToUpper() + " ****";
        Transmit("");
        Transmit(header);
    }

    
    public void LogStudyHeader(Options opts, string studyName)
    {
        string dividerLine = new string('=', 70);
        LogLine("");
        LogLine(dividerLine);
        LogLine(studyName);
        LogLine(dividerLine);
        LogLine("");
    }


    public void LogError(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_prefix + "***ERROR*** " + message;
        LogLine("");
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine(error_message);
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine("");
    }


    public void LogCodeError(string header, string errorMessage, string? stackTrace)
    {
        string headerMessage = "***ERROR*** " + header;
        LogLine("");
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine(headerMessage);
        LogLine(errorMessage + "\n");
        LogLine(stackTrace ?? "");
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine("");
    }
 

    public void LogParseError(string header, string errorNum, string errorType)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_prefix + "***ERROR*** " + "Error " + errorNum + ": " + header + " "  + errorType;
        LogLine(error_message);
    }


    public void LogTableStatistics(Source s, string schema)
    {
        // Gets and logs record count for each table in the sd schema of the database
        // Start by obtaining the connection string, then construct log line for each by 
        // calling db interrogation for each applicable table
        string db_conn = s.db_conn ?? "";

        LogLine("");
        LogLine("TABLE RECORD NUMBERS");

        if (s.has_study_tables is true)
        {
            LogHeader("study tables");
            LogLine("");
            LogLine(StudyTableSummary(db_conn, schema, "studies", false));
            LogLine(StudyTableSummary(db_conn, schema, "study_identifiers"));
            LogLine(StudyTableSummary(db_conn, schema, "study_titles"));

            // these are database dependent
            if (s.has_study_topics is true) LogLine(StudyTableSummary(db_conn, schema, "study_topics"));
            if (s.has_study_features is true) LogLine(StudyTableSummary(db_conn, schema, "study_features"));
            if (s.has_study_conditions is true) LogLine(StudyTableSummary(db_conn, schema, "study_conditions"));
            if (s.has_study_organisations is true) LogLine(StudyTableSummary(db_conn, schema, "study_organisations"));
            if (s.has_study_people is true) LogLine(StudyTableSummary(db_conn, schema, "study_people"));
            if (s.has_study_references is true) LogLine(StudyTableSummary(db_conn, schema, "study_references"));
            if (s.has_study_relationships is true) LogLine(StudyTableSummary(db_conn, schema, "study_relationships"));
            if (s.has_study_links is true) LogLine(StudyTableSummary(db_conn, schema, "study_links"));
            if (s.has_study_ipd_available is true) LogLine(StudyTableSummary(db_conn, schema, "study_ipd_available"));
            if (s.has_study_countries is true) LogLine(StudyTableSummary(db_conn, schema, "study_countries"));
            if (s.has_study_locations is true) LogLine(StudyTableSummary(db_conn, schema, "study_locations"));
            if (s.has_study_iec is true)
            {
                if (s.study_iec_storage_type! == "Single Table")
                {
                    LogLine(StudyTableSummary(db_conn, schema, "study_iec"));
                }
                else
                {
                    if (s.study_iec_storage_type! == "By Year Groupings")
                    {
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_upto12"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_13to19"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_20on"));
                    }
                    else if (s.study_iec_storage_type! == "By Years")
                    {
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_null"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_pre06"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_0608"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_0910"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_1112"));
                        LogLine(StudyTableSummary(db_conn, schema, "study_iec_1314"));
                        for (int i = 15; i < 30; i++)
                        {
                            LogLine(StudyTableSummary(db_conn, schema, $"study_iec_{i}"));
                        }
                    }
                }
            }
        }

        LogHeader("object tables");
        LogLine("");
        // these common to all databases
        LogLine(ObjectTableSummary(db_conn, schema, "data_objects", false));
        LogLine(ObjectTableSummary(db_conn, schema, "object_titles"));

        // these are database dependent		

        if (s.has_object_instances is true) LogLine(ObjectTableSummary(db_conn, schema, "object_instances"));
        if (s.has_object_datasets is true) LogLine(ObjectTableSummary(db_conn, schema, "object_datasets"));
        if (s.has_object_dates is true) LogLine(ObjectTableSummary(db_conn, schema, "object_dates"));
        if (s.has_object_relationships is true) LogLine(ObjectTableSummary(db_conn, schema, "object_relationships"));
        if (s.has_object_rights is true) LogLine(ObjectTableSummary(db_conn, schema, "object_rights"));

        if (s.has_journal_details is true) LogLine(ObjectTableSummary(db_conn, schema, "journal_details"));
        if (s.has_object_organisations is true) LogLine(ObjectTableSummary(db_conn, schema, "object_organisations"));
        if (s.has_object_people is true) LogLine(ObjectTableSummary(db_conn, schema, "object_people"));
        if (s.has_object_topics is true) LogLine(ObjectTableSummary(db_conn, schema, "object_topics"));
        if (s.has_object_comments is true) LogLine(ObjectTableSummary(db_conn, schema, "object_comments"));
        if (s.has_object_descriptions is true) LogLine(ObjectTableSummary(db_conn, schema, "object_descriptions"));
        if (s.has_object_identifiers is true) LogLine(ObjectTableSummary(db_conn, schema, "object_identifiers"));
        if (s.has_object_db_links is true) LogLine(ObjectTableSummary(db_conn, schema, "object_db_links"));
        if (s.has_object_publication_types is true) LogLine(ObjectTableSummary(db_conn, schema, "object_publication_types"));
        if (s.has_object_descriptions is true) LogLine(ObjectTableSummary(db_conn, schema, "object_descriptions"));

    }

    
    public void CloseLog()
    {
        if (_sw is not null)
        {
            LogHeader("Closing Log");
            _sw.Flush();
            _sw.Close();
        }
        
        // Write out the summary file.
        
        //var sw_summary = new StreamWriter(_summaryLogfilePath, true, System.Text.Encoding.UTF8);
        
        //sw_summary.Flush();
        //sw_summary.Close();
    }


    private void Transmit(string message)
    {
        _sw?.WriteLine(message);
        Console.WriteLine(message);
    }


    private string StudyTableSummary(string dbConn, string schema, string tableName, bool includeSource = true)
    {
        using NpgsqlConnection conn = new(dbConn);
        string sql_string = "select count(*) from " + schema + "." + tableName;
        int res = conn.ExecuteScalar<int>(sql_string);
        if (includeSource)
        {
            sql_string = "select count(distinct sd_sid) from " + schema + "." + tableName;
            int study_num = conn.ExecuteScalar<int>(sql_string);
            string study_word = study_num > 1 ? "studies" : "study";
            return $"{res} records found in {schema}.{tableName}, from {study_num} {study_word}";
        }
        return $"{res} records found in {schema}.{tableName}";
    }


    private string ObjectTableSummary(string dbConn, string schema, string tableName, bool includeSource = true)
    {
        using NpgsqlConnection conn = new(dbConn);
        string sql_string = "select count(*) from " + schema + "." + tableName;
        int res = conn.ExecuteScalar<int>(sql_string);
        if (includeSource)
        {
            sql_string = "select count(distinct sd_oid) from " + schema + "." + tableName;
            int object_num = conn.ExecuteScalar<int>(sql_string);
            string object_word = object_num > 1 ? "objects" : "object";
            return $"{res} records found in {schema}.{tableName}, from {object_num} {object_word}";
        }
        return $"{res} records found in {schema}.{tableName}";
    }

    public void SendEmail(string errorMessageText)
    {
        // construct txt file with message
        // and place in pickup folder for
        // SMTP service (if possible - may need to change permissions on folder)


    }


    public void SendRes(string resultText)
    {
        // construct txt file with message
        // and place in pickup folder for
        // SMTP service (if possible - may need to change permissions on folder)


    }

}

