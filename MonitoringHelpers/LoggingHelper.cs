using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using Dapper;
using Npgsql;
using System.Collections.Generic;

namespace DataHarvester
{
    public class LoggingHelper
    {
        private string logfile_startofpath;
        private string logfile_path;
        private StreamWriter sw;

        public LoggingHelper(string sourceName)
        {
            IConfigurationRoot settings = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            logfile_startofpath = settings["logfilepath"];

            string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                              .Replace(":", "").Replace("T", " ");

            string log_folder_path = Path.Combine(logfile_startofpath, sourceName);
            if (!Directory.Exists(log_folder_path))
            {
                Directory.CreateDirectory(log_folder_path);
            }

            logfile_path = Path.Combine(log_folder_path, "HV " + sourceName + " " + dt_string + ".log");
            sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
        }


        public string LogFilePath => logfile_path;

        
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

            int[] source_ids = opts.source_ids.ToArray();
            if (source_ids.Length == 1)
            {
                LogLine("Source_id is " + source_ids[0].ToString());
            }
            else
            {
                LogLine("Source_ids are " + string.Join(",", source_ids));
            }
            LogLine("Type_id is " + opts.harvest_type_id.ToString());
            LogLine("Update org ids only is " + opts.org_update_only);
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


        public void LogCodeError(string header, string errorMessage, string stackTrace)
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
            string db_conn = s.db_conn;

            LogLine("");
            LogLine("TABLE RECORD NUMBERS");

            if (s.has_study_tables)
            {
                LogLine("");
                LogLine("study tables...\n");
                LogLine(GetTableRecordCount(db_conn, schema, "studies"));
                LogLine(GetTableRecordCount(db_conn, schema, "study_identifiers"));
                LogLine(GetTableRecordCount(db_conn, schema, "study_titles"));

                // these are database dependent
                if (s.has_study_topics) LogLine(GetTableRecordCount(db_conn, schema, "study_topics"));
                if (s.has_study_features) LogLine(GetTableRecordCount(db_conn, schema, "study_features"));
                if (s.has_study_contributors) LogLine(GetTableRecordCount(db_conn, schema, "study_contributors"));
                if (s.has_study_references) LogLine(GetTableRecordCount(db_conn, schema, "study_references"));
                if (s.has_study_relationships) LogLine(GetTableRecordCount(db_conn, schema, "study_relationships"));
                if (s.has_study_links) LogLine(GetTableRecordCount(db_conn, schema, "study_links"));
                if (s.has_study_ipd_available) LogLine(GetTableRecordCount(db_conn, schema, "study_ipd_available"));
                if (s.has_study_countries) LogLine(GetTableRecordCount(db_conn, schema, "study_countries"));
                if (s.has_study_locations) LogLine(GetTableRecordCount(db_conn, schema, "study_locations"));

                LogLine(GetTableRecordCount(db_conn, schema, "study_hashes"));
                IEnumerable<hash_stat> study_hash_stats = (GetHashStats(db_conn, schema, "study_hashes"));
                if (study_hash_stats.Count() > 0)
                {
                    LogLine("");
                    LogLine("from the hashes...\n");
                    foreach (hash_stat hs in study_hash_stats)
                    {
                        LogLine(hs.num.ToString() + " study records have " + hs.hash_type + " (" + hs.hash_type_id.ToString() + ")");
                    }
                }
            }
            LogLine("");
            LogLine("object tables...\n");
            // these common to all databases
            LogLine(GetTableRecordCount(db_conn, schema, "data_objects"));
            LogLine(GetTableRecordCount(db_conn, schema, "object_instances"));
            LogLine(GetTableRecordCount(db_conn, schema, "object_titles"));

            // these are database dependent		

            if (s.has_object_datasets) LogLine(GetTableRecordCount(db_conn, schema, "object_datasets"));
            if (s.has_object_dates) LogLine(GetTableRecordCount(db_conn, schema, "object_dates"));
            if (s.has_object_relationships) LogLine(GetTableRecordCount(db_conn, schema, "object_relationships"));
            if (s.has_object_rights) LogLine(GetTableRecordCount(db_conn, schema, "object_rights"));
            if (s.has_object_pubmed_set)
            {
                LogLine(GetTableRecordCount(db_conn, schema, "journal_details"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_contributors"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_topics"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_comments"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_descriptions"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_identifiers"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_db_links"));
                LogLine(GetTableRecordCount(db_conn, schema, "object_publication_types"));
            }

            LogLine(GetTableRecordCount(db_conn, schema, "object_hashes"));
            IEnumerable<hash_stat> object_hash_stats = (GetHashStats(db_conn, schema, "object_hashes"));
            if (object_hash_stats.Count() > 0)
            {
                LogLine("");
                LogLine("from the hashes...\n");
                foreach (hash_stat hs in object_hash_stats)
                {
                    LogLine(hs.num.ToString() + " object records have " + hs.hash_type + " (" + hs.hash_type_id.ToString() + ")");
                }
            }
        }

        public void Reattach()
        {
            sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
        }

        public void SwitchLog()
        {
            LogHeader("Switching Log File Control");
            sw.Flush();
            sw.Close();
        }


        public void CloseLog()
        {
            LogHeader("Closing Log");
            sw.Flush();
            sw.Close();
        }


        private void Transmit(string message)
        {
            sw.WriteLine(message);
            Console.WriteLine(message);
        }


        private string GetTableRecordCount(string db_conn, string schema, string table_name)
        {
            string sql_string = "select count(*) from " + schema + "." + table_name;

            using (NpgsqlConnection conn = new NpgsqlConnection(db_conn))
            {
                int res = conn.ExecuteScalar<int>(sql_string);
                return res.ToString() + " records found in " + schema + "." + table_name;
            }
        }


        private IEnumerable<hash_stat> GetHashStats(string db_conn, string schema, string table_name)
        {
            string sql_string = "select hash_type_id, hash_type, count(id) as num from " + schema + "." + table_name;
            sql_string += " group by hash_type_id, hash_type order by hash_type_id;";

            using (NpgsqlConnection conn = new NpgsqlConnection(db_conn))
            {
                return conn.Query<hash_stat>(sql_string);
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
}

