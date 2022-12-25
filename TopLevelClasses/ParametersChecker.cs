using CommandLine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MDR_Harvester;

internal class ParameterChecker
{
    private IMonitorDataLayer _mon_repo;
    private ITestingDataLayer _test_repo;
    private LoggingHelper _logging_helper;

    internal ParameterChecker(IMonitorDataLayer mon_repo, ITestingDataLayer test_repo, LoggingHelper logging_helper)
    {
        _mon_repo = mon_repo;
        _test_repo = test_repo;
        _logging_helper = logging_helper;
    }


    internal ParamsCheckResult CheckParams(string[]? args)
    {
        // Calls the CommandLine parser. If an error in the initial parsing, log it 
        // and return an error. If parameters can be passed, check their validity
        // and if invalid log the issue and return an error, otherwise return the 
        // parameters, processed as an instance of the Options class, and the source.

        var parsedArguments = Parser.Default.ParseArguments<Options>(args);
        if (parsedArguments.Errors.Any())
        {
            LogParseError(((NotParsed<Options>)parsedArguments).Errors);
            return new ParamsCheckResult(true, false, null);
        }
        else
        {
            var opts = parsedArguments.Value;
            return CheckArgumentValuesAreValid(opts);
        }
    }


    internal ParamsCheckResult CheckArgumentValuesAreValid(Options opts)
    {
        // 'opts' is passed by reference and may be changed by the checking mechanism.

        try
        {
            if (opts.setup_expected_data_only)
            {
                // Set the 'manual input of test data' source id.

                List<int> ids = new() { 999999 };
                opts.source_ids = ids;
                opts.harvest_type_id = 3;
                opts.org_update_only = false;
                return new ParamsCheckResult(false, false, opts); // can always run if -E parameter present
            }
            else if (opts.harvest_all_test_data)
            {
                // Set up array of source ids to reflect
                // those in the test data set.

                opts.source_ids = _test_repo.ObtainTestSourceIDs();
                opts.harvest_type_id = 3;
                opts.org_update_only = false;
                return new ParamsCheckResult(false, false, opts); // should always run if -F parameter present
            }
            else
            {
                // check valid harvest type id

                int harvest_type_id = opts.harvest_type_id;
                if (!opts.org_update_only)
                {
                    if (harvest_type_id != 1 && harvest_type_id != 2 && harvest_type_id != 3)
                    {
                        throw new ArgumentException("The t (harvest type) parameter is not one of the allowed values - 1,2 or 3");
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

                // parameters valid - return opts and the source.

                return new ParamsCheckResult(false, false, opts);
            }
        }

        catch (Exception e)
        {
            _logging_helper.OpenNoSourceLogFile();
            _logging_helper.LogHeader("INVALID PARAMETERS");
            _logging_helper.LogCommandLineParameters(opts);
            _logging_helper.LogCodeError("MDR_Harvester application aborted", e.Message, e.StackTrace ?? "");
            _logging_helper.CloseLog();
            return new ParamsCheckResult(false, true, null);
        }
    }


    internal void LogParseError(IEnumerable<Error> errs)
    {
        _logging_helper.OpenNoSourceLogFile();
        _logging_helper.LogHeader("UNABLE TO PARSE PARAMETERS");
        _logging_helper.LogHeader("Error in input parameters");
        _logging_helper.LogLine("Error in the command line arguments - they could not be parsed");

        int n = 0;
        foreach (Error e in errs)
        {
            n++;
            _logging_helper.LogParseError("Error {n}: Tag was {Tag}", n.ToString(), e.Tag.ToString());
            if (e.GetType().Name == "UnknownOptionError")
            {
                _logging_helper.LogParseError("Error {n}: Unknown option was {UnknownOption}", n.ToString(), ((UnknownOptionError)e).Token);
            }
            if (e.GetType().Name == "MissingRequiredOptionError")
            {
                _logging_helper.LogParseError("Error {n}: Missing option was {MissingOption}", n.ToString(), ((MissingRequiredOptionError)e).NameInfo.NameText);
            }
            if (e.GetType().Name == "BadFormatConversionError")
            {
                _logging_helper.LogParseError("Error {n}: Wrongly formatted option was {MissingOption}", n.ToString(), ((BadFormatConversionError)e).NameInfo.NameText);
            }
        }
        _logging_helper.LogLine("MDR_Downloader application aborted");
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


internal class ParamsCheckResult
{
    internal bool ParseError { get; set; }
    internal bool ValidityError { get; set; }
    internal Options? Pars { get; set; }

    internal ParamsCheckResult(bool _ParseError, bool _ValidityError, Options? _Pars)
    {
        ParseError = _ParseError;
        ValidityError = _ValidityError;
        Pars = _Pars;
    }
}






