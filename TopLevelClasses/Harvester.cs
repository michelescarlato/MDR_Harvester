using MDR_Harvester.Biolincc;
using MDR_Harvester.Ctg;
using MDR_Harvester.Euctr;
using MDR_Harvester.Isrctn;
using MDR_Harvester.Pubmed;
using MDR_Harvester.Who;
using MDR_Harvester.Yoda;

namespace MDR_Harvester;

class Harvester
{

    private readonly ILoggingHelper _loggingHelper;    
    private readonly IMonDataLayer _monDataLayer;
    private readonly IStorageDataLayer _storageDataLayer;


    public Harvester(ILoggingHelper logging_helper, IMonDataLayer monDataLayer, IStorageDataLayer storageDataLayer)
    {
        _loggingHelper = logging_helper;
        _monDataLayer = monDataLayer;
   
        _storageDataLayer = storageDataLayer;
    }

    public void Run(Options opts)
    {
        try
        {
            // Simply harvest the data for each listed source.
                
            foreach (int source_id in opts.source_ids!)
            {
                // Obtain source details, augment with connection string for this database
                // Open up the logging file for this source and then call the main 
                // harvest routine. After initial checks source is guaranteed to be non-null.
                
                Source source = _monDataLayer.FetchSourceParameters(source_id);
                string dbName = source.database_name!;
                source.db_conn = _monDataLayer.GetConnectionString(dbName);

                _loggingHelper.OpenLogFile(dbName);
                _loggingHelper.LogCommandLineParameters(opts);
                _loggingHelper.LogHeader("STARTING HARVESTER");
                _loggingHelper.LogStudyHeader(opts, "For source: " + source.id + ": " + dbName);

                HarvestData(source, opts);

                _loggingHelper.LogTableStatistics(source, "sd");
                _loggingHelper.CloseLog();
            }
        }

        catch (Exception e)
        {
            _loggingHelper.LogHeader("UNHANDLED EXCEPTION");
            _loggingHelper.LogCodeError("Harvester application aborted", e.Message, e.StackTrace);
            _loggingHelper.CloseLog();
        }
    }


    private void HarvestData(Source source, Options opts)
    {
        // Construct the sd tables. (Some sources may be data objects only.)
        // Type 3 does not disturb the tables, other than replacing the existing data for
        // the 'for testing' studies / objects and their attributes.
        // Type 4 is for restarting / continuing an exiting harvest after an error and so
        //  existing tables are not recreated. Type 4 not used in normal processing.

        if (opts.harvest_type_id is 1 or 2)
        {
            _loggingHelper.LogHeader("Recreate database tables");
            SchemaBuilder sdb = new(source, _loggingHelper);
            sdb.RecreateTables();
        }
        if (opts.harvest_type_id is 3)
        {
            _loggingHelper.LogHeader("Deleting existing 'for testing' data");
            SchemaBuilder sdb = new(source, _loggingHelper);
            //sdb.DeleteTestData();
        }
        

        // Construct the harvest_event record.

        int source_id = source.id;
        int harvest_id = _monDataLayer.GetNextHarvestEventId();
        HarvestEvent harvest = new(harvest_id, source_id, opts.harvest_type_id);
        _loggingHelper.LogLine("Harvest event " + harvest_id + " began");

        // Harvest the data from the local JSON files.

        _loggingHelper.LogHeader("Process data");
        IStudyProcessor? study_processor = null;
        IObjectProcessor? object_processor = null;
        harvest.num_records_available = _monDataLayer.FetchFullFileCount(opts.harvest_type_id);

        if (source.source_type == "study")
        {
            if (source.uses_who_harvest is true)
            {
                study_processor = new WHOProcessor();
            }
            else
            {
                if (source.id is 101900)
                {
                    study_processor = new BioLinccProcessor();
                }
                else if (source.id is 101901)
                {
                    study_processor = new YodaProcessor();
                }
                else if (source.id is 100120)
                {
                    study_processor = new CTGProcessor();
                } 
                else if (source.id is 100123)
                {
                    study_processor = new EUCTRProcessor();
                } 
                else if (source.id is 100126)
                {
                    study_processor = new IsrctnProcessor();
                } 
            }
            if (study_processor is not null)
            {
                StudyController c = new(_loggingHelper, _monDataLayer, _storageDataLayer, source, opts, study_processor);
                harvest.num_records_harvested = c.LoopThroughFiles(opts.harvest_type_id, harvest_id);
            }
        }
        else
        {
            // Source type is 'object'.
            if (source.id is 100135)
            {
                object_processor = new PubmedProcessor();
            }
            if (object_processor is not null)
            {
                ObjectController c = new(_loggingHelper, _monDataLayer, _storageDataLayer, source,
                    object_processor);
                harvest.num_records_harvested = c.LoopThroughFiles(opts.harvest_type_id, harvest_id);
            }
        }

        harvest.time_ended = DateTime.Now;
        _monDataLayer.StoreHarvestEvent(harvest);

        _loggingHelper.LogLine("Number of source JSON files: " + harvest.num_records_available.ToString());
        _loggingHelper.LogLine("Number of files harvested: " + harvest.num_records_harvested.ToString());
        _loggingHelper.LogLine("Harvest event " + harvest_id.ToString() + " ended");
    }
}


