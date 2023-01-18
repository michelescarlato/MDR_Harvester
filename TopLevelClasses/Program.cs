using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MDR_Harvester;


// Set up file based configuration environment.
// ensuring the system can read assembly location OK.

string AssemblyLocation = Assembly.GetExecutingAssembly().Location;
string? BasePath = Path.GetDirectoryName(AssemblyLocation);
if (!string.IsNullOrWhiteSpace(BasePath))
{
    var configFiles = new ConfigurationBuilder()
        .SetBasePath(BasePath)
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
        .Build();

    // Set up the host for the app,
    // adding the services used in the system to support DI
                // Register services (or develop a comp root)

    IHost host = Host.CreateDefaultBuilder()
            .UseContentRoot(BasePath)
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddConfiguration(configFiles);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<ICredentials, Credentials>();
                services.AddSingleton<IHarvester, Harvester>();
                services.AddSingleton<IMonitorDataLayer, MonitorDataLayer>();
                services.AddSingleton<IStorageDataLayer, StorageDataLayer>();
                services.AddSingleton<ITestingDataLayer, TestingDataLayer>();
                services.AddTransient<ISource, Source>();
            })
            .Build();


    LoggingHelper _logging_helper = new();      
    
    // N.r. logging helper created without explicit source at this stage.
    // If needed in initial phase will open as a 'no source' log file.
    // Otherwise separate file opened per source, later, as required.

    ParameterChecker _param_checker = ActivatorUtilities.CreateInstance<ParameterChecker>(host.Services, _logging_helper);
    
    ParamsCheckResult paramsCheck = _param_checker.CheckParams(args);
    if (paramsCheck.ParseError || paramsCheck.ValidityError)
    {
        return -1;  // end program, any parameter errors should have been logged
    }
    else
    {
        try
        {
            // Should be able to proceed - (opts are known to be non-null)

            var opts = paramsCheck.Pars!;
            Harvester harvester = ActivatorUtilities.CreateInstance<Harvester>(host.Services);
            harvester.Run(opts);

            return 0;
        }
        catch (Exception e)
        {
            // if an error bubbles up to here there is an issue with the code.

            _logging_helper.LogHeader("UNHANDLED EXCEPTION");
            _logging_helper.LogCodeError("MDR_Harvester application aborted", e.Message, e.StackTrace);
            _logging_helper.CloseLog();
            return -1;
        }
    }
}
else
{
    return -1;
}

