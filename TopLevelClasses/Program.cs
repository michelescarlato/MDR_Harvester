using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MDR_Harvester;

string assemblyLocation = Assembly.GetExecutingAssembly().Location;
string? basePath = Path.GetDirectoryName(assemblyLocation);
if (string.IsNullOrWhiteSpace(basePath))
{
    return -1;
}

var configFiles = new ConfigurationBuilder()
.SetBasePath(basePath)
.AddJsonFile("appsettings.json")
.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
.Build();

// Set up the host for the app,
// adding the services used in the system to support DI.
// Note ALL listed services are singletons.

IHost host = Host.CreateDefaultBuilder()
.UseContentRoot(basePath)
.ConfigureAppConfiguration(builder =>
{
    builder.AddConfiguration(configFiles);
})
.ConfigureServices((services) =>
{
    services.AddSingleton<ICredentials, Credentials>();
    services.AddSingleton<ILoggingHelper, LoggingHelper>();
    services.AddSingleton<IMonDataLayer, MonDataLayer>();
    services.AddSingleton<ITestDataLayer, TestDataLayer>();    
    services.AddSingleton<IStorageDataLayer, StorageDataLayer>();
    services.AddSingleton<IStudyCopyHelpers, StudyCopyHelpers>();
    services.AddSingleton<IObjectCopyHelpers, ObjectCopyHelpers>();

})
.Build();

// Establish loggingHelper, at this stage as an object reference
// because the log file(s) are yet to be opened.
// Establish the repository classes using the services above.

LoggingHelper loggingHelper = ActivatorUtilities.CreateInstance<LoggingHelper>(host.Services);
MonDataLayer monDataLayer = ActivatorUtilities.CreateInstance<MonDataLayer>(host.Services);
TestDataLayer testDataLayer = ActivatorUtilities.CreateInstance<TestDataLayer>(host.Services);
StorageDataLayer storageDataLayer = ActivatorUtilities.CreateInstance<StorageDataLayer>(host.Services);

// A parameter checker is instantiated and first checks if the program's arguments 
// can be parsed and if they can then checks if they are valid.
// If both tests are passed the object returned includes both the
// original arguments and the 'source' object with details of the
// single data source being downloaded. 

ParameterChecker paramChecker = new(loggingHelper, monDataLayer, testDataLayer);
ParamsCheckResult paramsCheck = paramChecker.CheckParams(args);
if (paramsCheck.ParseError || paramsCheck.ValidityError)
{
    // End program, parameter errors should have been logged
    // in a 'no source' file by the ParameterChecker class.
    return -1;
}

// Should be able to proceed - (opts and source are known to be non-null).
// Open log file, create Harvester class and call the main harvest function

try
{
    var opts = paramsCheck.Pars!;
    Harvester harvester = new(loggingHelper, monDataLayer, testDataLayer, storageDataLayer);
    harvester.Run(opts);
    return 0;
}
catch (Exception e)
{
    // If an error bubbles up to here there is an unexpected issue with the code.
    // A file should normally have been created (but just in case...).

    if (loggingHelper.LogFilePath == "")
    {
        loggingHelper.OpenNoSourceLogFile();
    }
    loggingHelper.LogHeader("UNHANDLED EXCEPTION");
    loggingHelper.LogCodeError("MDR_Harvester application aborted", e.Message, e.StackTrace);
    loggingHelper.CloseLog();
    return -1;
}


