using CommandLine;
using System;
using System.Collections.Generic;

namespace DataHarvester
{
    internal class ParametersChecker : IParametersChecker
    {
        private IMonitorDataLayer _mon_repo;
        private ITestingDataLayer _test_repo;
        private LoggingHelper _logging_helper;

        public ParametersChecker(IMonitorDataLayer mon_repo, ITestingDataLayer test_repo)
        {
            _mon_repo = mon_repo;
            _test_repo = test_repo;
        }

        // Parse command line arguments and return true only if no errors.
        // Otherwise log errors and return false.

        public Options ObtainParsedArguments(string[] args)
        {
            var parsedArguments = Parser.Default.ParseArguments<Options>(args);
            if (parsedArguments.Tag.ToString() == "NotParsed")
            {
                HandleParseError(((NotParsed<Options>)parsedArguments).Errors);
                return null;
            }
            else
            {
                return ((Parsed<Options>)parsedArguments).Value;
            }
        }

        // Parse command line arguments and return true if values are valid.
        // Otherwise log errors and return false.

        public bool ValidArgumentValues(Options opts)
        {
            try
            {
                if (opts.setup_expected_data_only)
                {
                    // Set the 'manual input of test data' source id.

                    List<int> ids = new List<int>();
                    ids.Add(999999);
                    opts.source_ids = ids;
                    opts.harvest_type_id = 3;
                    opts.org_update_only = false;
                    return true; // can always run if -E parameter present
                }
                else if (opts.harvest_all_test_data)
                {
                   // Set up array of source ids to reflect
                   // those in the test data set.

                   opts.source_ids = _test_repo.ObtainTestSourceIDs();
                   opts.harvest_type_id = 3;
                   opts.org_update_only = false;
                   return true; // should always run if -F parameter present
                }
                else
                { 
                    // check valid harvest type id

                    int harvest_type_id = opts.harvest_type_id;
                    if (!opts.org_update_only)
                    {
                        if (harvest_type_id != 1 && harvest_type_id != 2 && harvest_type_id != 3)
                        {
                            throw new Exception("The t (harvest type) parameter is not one of the allowed values - 1,2 or 3");
                        }
                    }

                    // check the source(s) validity

                    foreach (int source_id in opts.source_ids)
                    {
                        if (!_mon_repo.SourceIdPresent(source_id))
                        {
                            throw new ArgumentException("Source argument " + source_id.ToString() +
                                                        " does not correspond to a known source");
                        }
                    }

                    return true;    // Got this far - the program can run!
                }
            }

            catch (Exception e)  
            {
                _logging_helper = new LoggingHelper("no source");
                _logging_helper.LogHeader("INVALID PARAMETERS");
                _logging_helper.LogCommandLineParameters(opts);
                _logging_helper.LogCodeError("Harvester application aborted", e.Message, e.StackTrace);
                _logging_helper.CloseLog();
                return false;
            }

        }


        private void HandleParseError(IEnumerable<Error> errs)
        {
            // log the errors

            _logging_helper = new LoggingHelper("no source");
            _logging_helper.LogHeader("UNABLE TO PARSE PARAMETERS");
            _logging_helper.LogHeader("Error in input parameters");
            _logging_helper.LogLine("Error in the command line arguments - they could not be parsed");

            int n = 0;
            foreach (Error e in errs)
            {
                n++;
                _logging_helper.LogParseError("Tag was ", n.ToString(), e.Tag.ToString());

                if (e.GetType().Name == "UnknownOptionError")
                {
                    _logging_helper.LogParseError("Unknown option was ", n.ToString(), ((UnknownOptionError)e).Token);
                }
                if (e.GetType().Name == "MissingRequiredOptionError")
                {
                    _logging_helper.LogParseError("Missing option was ", n.ToString(), ((MissingRequiredOptionError)e).NameInfo.NameText);
                }
                if (e.GetType().Name == "BadFormatConversionError")
                {
                    _logging_helper.LogParseError("Wrongly formatted option was ", n.ToString(), ((BadFormatConversionError)e).NameInfo.NameText);
                }
            }
            _logging_helper.LogLine("Harvester application aborted");
            _logging_helper.CloseLog();
        }

    }


    public class Options
    {
        // Lists the command line arguments and options

        [Option('s', "source_ids", Required = false, Separator = ',', HelpText = "Comma separated list of Integer ids of data sources.")]
        public IEnumerable<int> source_ids { get; set; }

        [Option('t', "harvest_type_id", Required = false, HelpText = "Integer representing type of harvest (1 = full, i.e. all available files, 2 = only files downloaded since last import, 3 = test data only.")]
        public int harvest_type_id { get; set; }

        [Option('G', "organisation_update_only", Required = false, HelpText = "If present does not recreate sd tables - only updates organisation ids")]
        public bool org_update_only { get; set; }

        [Option('E', "establish_expected_test_data", Required = false, HelpText = "If present only creates and fills tables for the 'expected' data. for comparison with processed test data")]
        public bool setup_expected_data_only { get; set; }

        [Option('F', "harvest_all_test_data", Required = false, HelpText = "If present only creates and fills tables for the designated test data, for comparison with expected test data")]
        public bool harvest_all_test_data { get; set; }
    }

}





