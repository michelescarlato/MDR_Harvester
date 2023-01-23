using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using Dapper;
using Npgsql;
using System.Collections.Generic;

namespace MDR_Harvester;

public class LoggingHelper : ILoggingHelper
{
    private string logfile_startofpath;
    private string summary_logfile_startofpath;
    private string logfile_path = "";
    private string summary_logfile_path = "";
    string dt_string;

    private StreamWriter? sw;

    public LoggingHelper()
    {
        IConfigurationRoot settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                          .Replace(":", "").Replace("T", " ");

        logfile_startofpath = settings["logfilepath"] ?? "";
        summary_logfile_startofpath = settings["summaryfilepath"] ?? "";
    }


    // Used to check if a log file with a named source has been created.

    public string LogFilePath => logfile_path;


    public void OpenLogFile(string database_name)
    {
        string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                          .Replace(":", "").Replace("T", " ");

        string log_folder_path = Path.Combine(logfile_startofpath, database_name);
        if (!Directory.Exists(log_folder_path))
        {
            Directory.CreateDirectory(log_folder_path);
        }

        logfile_path = Path.Combine(log_folder_path, "DL " + database_name + " " + dt_string);
        summary_logfile_path = Path.Combine(summary_logfile_startofpath, "DL " + database_name + " " + dt_string);

        // source file name used for WHO case, where the source is a file
        // In other cases is not required

        logfile_path += ".log";
        sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
    }



    public void OpenNoSourceLogFile()
    {
        logfile_path += logfile_startofpath + "HV Source not set " + dt_string + ".log";
        sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
    }
    

    public void LogCommandLineParameters(Options opts)
    {
        if (opts.harvest_all_test_data)
        {
            LogHeader("HARVESTING ALL TEST DATA");
        }

        if (opts.setup_expected_data_only)
        {
            LogHeader("HARVESTING EXPECTED (MANUAL INPUT) DATA");
        }

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
        string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string feedback = dt_string + message + identifier;
        Transmit(feedback);
    }


    public void LogStudyHeader(Options opts, string studyName)
    {
        int harvest_type = opts.harvest_type_id;
        string dividerline = "";
        if (opts.harvest_all_test_data || opts.setup_expected_data_only)
        {
            dividerline = new string('-', 70);
        }
        else
        {
            dividerline = (harvest_type == 1 || harvest_type == 2) ? new string('=', 70) : new string('-', 70);
        }
        LogLine("");
        LogLine(dividerline);
        LogLine(studyName);
        LogLine(dividerline);
        LogLine("");
    }


    public void LogHeader(string message)
    {
        string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string header = dt_string + "**** " + message.ToUpper().ToUpper() + " ****";
        Transmit("");
        Transmit(header);
    }


    public void LogError(string message)
    {
        string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_string + "***ERROR*** " + message;
        Transmit("");
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit(error_message);
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit("");
    }


    public void LogCodeError(string header, string errorMessage, string? stackTrace)
    {
        string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string headerMessage = dt_string + "***ERROR*** " + header + "\n";
        Transmit("");
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit(headerMessage);
        Transmit(errorMessage + "\n");
        Transmit(stackTrace);
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit("");
    }
 

    public void LogParseError(string header, string errorNum, string errorType)
    {
        string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_string + "***ERROR*** " + "Error " + errorNum + ": " + header + " "  + errorType;
        Transmit(error_message);
    }


    public void LogTableStatistics(ISource s, string schema)
    {
        // Gets and logs record count for each table in the sd schema of the database
        // Start by obtaining conection string, then construct log line for each by 
        // calling db interrogation for each applicable table
        string db_conn = s.db_conn ?? "";

        LogLine("");
        LogLine("TABLE RECORD NUMBERS");

        if (s.has_study_tables == true)
        {
            LogHeader("study tables");
            LogLine("");
            LogLine(StudyTableSummary(db_conn, schema, "studies", false));
            LogLine(StudyTableSummary(db_conn, schema, "study_identifiers"));
            LogLine(StudyTableSummary(db_conn, schema, "study_titles"));

            // these are database dependent
            if (s.has_study_topics == true) LogLine(StudyTableSummary(db_conn, schema, "study_topics"));
            if (s.has_study_features == true) LogLine(StudyTableSummary(db_conn, schema, "study_features"));
            if (s.has_study_conditions == true) LogLine(StudyTableSummary(db_conn, schema, "study_conditions"));
            if (s.has_study_iec == true) LogLine(StudyTableSummary(db_conn, schema, "study_iec"));
            if (s.has_study_contributors == true) LogLine(StudyTableSummary(db_conn, schema, "study_contributors"));
            if (s.has_study_references == true) LogLine(StudyTableSummary(db_conn, schema, "study_references"));
            if (s.has_study_relationships == true) LogLine(StudyTableSummary(db_conn, schema, "study_relationships"));
            if (s.has_study_links == true) LogLine(StudyTableSummary(db_conn, schema, "study_links"));
            if (s.has_study_ipd_available == true) LogLine(StudyTableSummary(db_conn, schema, "study_ipd_available"));
            if (s.has_study_countries == true) LogLine(StudyTableSummary(db_conn, schema, "study_countries"));
            if (s.has_study_locations == true) LogLine(StudyTableSummary(db_conn, schema, "study_locations"));
        }

        LogHeader("object tables");
        LogLine("");
        // these common to all databases
        LogLine(ObjectTableSummary(db_conn, schema, "data_objects", false));
        LogLine(ObjectTableSummary(db_conn, schema, "object_instances"));
        LogLine(ObjectTableSummary(db_conn, schema, "object_titles"));

        // these are database dependent		

        if (s.has_object_datasets == true) LogLine(ObjectTableSummary(db_conn, schema, "object_datasets"));
        if (s.has_object_dates == true) LogLine(ObjectTableSummary(db_conn, schema, "object_dates"));
        if (s.has_object_relationships == true) LogLine(ObjectTableSummary(db_conn, schema, "object_relationships"));
        if (s.has_object_rights == true) LogLine(ObjectTableSummary(db_conn, schema, "object_rights"));
        if (s.has_object_pubmed_set == true)
        {
            LogLine(ObjectTableSummary(db_conn, schema, "journal_details"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_contributors"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_topics"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_comments"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_descriptions"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_identifiers"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_db_links"));
            LogLine(ObjectTableSummary(db_conn, schema, "object_publication_types"));
        }
    }

    public void Reattach()
    {
        sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
    }

    public void SwitchLog()
    {
        LogHeader("Switching Log File Control");
        sw?.Flush();
        sw?.Close();
    }


    public void CloseLog()
    {
        LogHeader("Closing Log");
        sw?.Flush();
        sw?.Close();
    }


    private void Transmit(string message)
    {
        sw?.WriteLine(message);
        Console.WriteLine(message);
    }


    private string StudyTableSummary(string db_conn, string schema, string table_name, bool include_source = true)
    {
        using NpgsqlConnection conn = new(db_conn);
        string sql_string = "select count(*) from " + schema + "." + table_name;
        int res = conn.ExecuteScalar<int>(sql_string);
        if (include_source)
        {
            sql_string = "select count(distinct sd_sid) from " + schema + "." + table_name;
            int study_num = conn.ExecuteScalar<int>(sql_string);
            return $"{res} records found in {schema}.{table_name}, from {study_num} studies";
        }
        else
        {
            return $"{res} records found in {schema}.{table_name}";
        }
    }


    private string ObjectTableSummary(string db_conn, string schema, string table_name, bool include_source = true)
    {
        using NpgsqlConnection conn = new(db_conn);
        string sql_string = "select count(*) from " + schema + "." + table_name;
        int res = conn.ExecuteScalar<int>(sql_string);
        if (include_source)
        {
            sql_string = "select count(distinct sd_oid) from " + schema + "." + table_name;
            int object_num = conn.ExecuteScalar<int>(sql_string);
            return $"{res} records found in {schema}.{table_name}, from {object_num} objects";
        }
        else
        {
            return $"{res} records found in {schema}.{table_name}";
        }
    }

    public void SendEmail(string error_message_text)
    {
        // construct txt file with message
        // and place in pickup folder for
        // SMTP service (if possible - may need to change permissions on folder)


    }


    public void SendRes(string result_text)
    {
        // construct txt file with message
        // and place in pickup folder for
        // SMTP service (if possible - may need to change permissions on folder)


    }

}

