using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MDR_Harvester;


string AssemblyLocation = Assembly.GetExecutingAssembly().Location;
string? BasePath = Path.GetDirectoryName(AssemblyLocation);
if (string.IsNullOrWhiteSpace(BasePath))
{
    return -1;
}

var configFiles = new ConfigurationBuilder()
.SetBasePath(BasePath)
.AddJsonFile("appsettings.json")
.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
.Build();

// Set up the host for the app,
// adding the services used in the system to support DI.
// Note all listed services are singletons apart from ISource.

IHost host = Host.CreateDefaultBuilder()
.UseContentRoot(BasePath)
.ConfigureAppConfiguration(builder =>
{
    builder.AddConfiguration(configFiles);
})
.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton<ICredentials, Credentials>();
    services.AddSingleton<ILoggingHelper, LoggingHelper>();
    services.AddSingleton<IMonDataLayer, MonDataLayer>();
    services.AddSingleton<IHarvester, Harvester>();
    services.AddSingleton<IStorageDataLayer, StorageDataLayer>();
    services.AddSingleton<ITestingDataLayer, TestingDataLayer>();
    services.AddTransient<ISource, Source>();
})
.Build();


// Establish loggingHelper, at this stage as an object reference
// because the log file(s) are yet to be opened.
// Establish a new parameter checker class.

LoggingHelper logging_helper = ActivatorUtilities.CreateInstance<LoggingHelper>(host.Services);
ParameterChecker paramChecker = ActivatorUtilities.CreateInstance<ParameterChecker>(host.Services);

// The parameter checker first checks if the program's arguments 
// can be parsed and if they can then checks if they are valid.
// If both tests are passed the object returned includes both the
// original arguments and the 'source' object with details of the
// single data source being downloaded. 

ParamsCheckResult paramsCheck = paramChecker.CheckParams(args);
if (paramsCheck.ParseError || paramsCheck.ValidityError)
{
    // End program, parameter errors should have been logged
    // in a 'no source' file by the ParameterChecker class.

    return -1;
}
else
{
    // Should be able to proceed - (opts and srce are known to be non-null).
    // Open log file, create Harvester class and call the main harvest function

    try
    {
        var opts = paramsCheck.Pars!;
        Harvester harvester = ActivatorUtilities.CreateInstance<Harvester>(host.Services);
        harvester.Run(opts);
        return 0;
    }
    catch (Exception e)
    {
        // If an error bubbles up to here there is an issue with the code.

        logging_helper.LogHeader("UNHANDLED EXCEPTION");
        logging_helper.LogCodeError("MDR_Harvester application aborted", e.Message, e.StackTrace);
        logging_helper.CloseLog();
        return -1;
    }
}

