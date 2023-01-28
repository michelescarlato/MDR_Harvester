using MDR_Harvester.Biolincc;
using MDR_Harvester.Ctg;
using MDR_Harvester.Euctr;
using MDR_Harvester.Isrctn;
using MDR_Harvester.Pubmed;
using MDR_Harvester.Who;
using MDR_Harvester.Yoda;

namespace MDR_Harvester;

class Harvester : IHarvester
{
    private readonly ILoggingHelper _logging_helper;
    private readonly IMonDataLayer _monDataLayer;
    private readonly IStorageDataLayer _storageDataLayer;
    private readonly ITestingDataLayer _testDataLayer;

    public Harvester(ILoggingHelper logging_helper, IMonDataLayer monDataLayer, IStorageDataLayer storageDataLayer, 
                     ITestingDataLayer testDataLayer)
    {
        _logging_helper = logging_helper;
        _monDataLayer = monDataLayer;
        _storageDataLayer = storageDataLayer;
        _testDataLayer = testDataLayer;
    }

    public int Run(Options opts)
    {
        try
        {
            foreach (int source_id in opts.source_ids!)
            {
                // Obtain source details, augment with connection string for this database

                ISource source = _monDataLayer.FetchSourceParameters(source_id);
                Credentials creds = _monDataLayer.Credentials;
                source.db_conn = creds.GetConnectionString(source.database_name, opts.harvest_type_id);

                // establish and begin the loggingHelper helper for this harvest

                _logging_helper!.OpenLogFile(source.database_name);
                _logging_helper.LogCommandLineParameters(opts);
                _logging_helper.LogHeader("STARTING HARVESTER");
                _logging_helper.LogStudyHeader(opts, "For source: " + source.id + ": " + source.database_name);

                // Call the main routine to do the harvesting, if not just a context data update

                HarvestData(source, opts);

                // If harvesting test data it needs to be transferred  
                // to the sdcomp schema for safekeeping and further processing
                // If a normal harvest from a full source statistics should be produced.
                // If the harvest was of the manual 'expected' data do neither.

                if (source.source_type != "test")
                {
                    if (opts.harvest_type_id == 3)
                    {
                        _testDataLayer.TransferTestSDData(source);
                    }
                    else
                    {
                        _logging_helper.LogTableStatistics(source, "sd");
                    }
                }

                _logging_helper.CloseLog();
            }

            return 0;
        }

        catch (Exception e)
        {
            _logging_helper.LogHeader("UNHANDLED EXCEPTION");
            _logging_helper.LogCodeError("Harvester application aborted", e.Message, e.StackTrace);
            _logging_helper.CloseLog();
            return -1;
        }
    }



    private void HarvestData(ISource source, Options opts)
    {
        if (source.source_type == "test")
        {
            // Set up expected data for later processing.
            // This is data derived from manual inspection of files and requires
            // a very different method, using stored procedures in the test db.

            _testDataLayer.EstablishExpectedData();
        }
        else
        {
            // Otherwise... Construct the sd tables. (Some sources may be data objects only.)

            _logging_helper.LogHeader("Recreate database tables");
            SchemaBuilder sdb = new(source, _logging_helper);
            sdb.RecreateTables();

            // Construct the harvest_event record.

            int source_id = source.id.HasValue ? (int)source.id : 0;
            int harvest_id = _monDataLayer.GetNextHarvestEventId();
            HarvestEvent harvest = new(harvest_id, source_id, opts.harvest_type_id);
            _logging_helper.LogLine("Harvest event " + harvest_id.ToString() + " began");

            // Harvest the data from the local JSON files.

            _logging_helper.LogHeader("Process data");
            IStudyProcessor? study_processor = null;
            IObjectProcessor? object_processor = null;
            harvest.num_records_available = _monDataLayer.FetchFullFileCount(source_id, source.source_type!, opts.harvest_type_id);

            if (source.source_type == "study")
            {
                if (source.uses_who_harvest == true)
                {
                    study_processor = new WHOProcessor(_logging_helper);
                }
                else
                {
                    switch (source.id)
                    {
                        case 101900:
                            {
                                study_processor = new BioLinccProcessor(_logging_helper);
                                break;
                            }
                        case 101901:
                            {
                                study_processor = new YodaProcessor(_logging_helper);
                                break;
                            }
                        case 100120:
                            {
                                study_processor = new CTGProcessor(_logging_helper);
                                break;
                            }
                        case 100123:
                            {
                                study_processor = new EUCTRProcessor(_logging_helper);
                                break;
                            }
                        case 100126:
                            {
                                study_processor = new IsrctnProcessor(_logging_helper);
                                break;
                            }
                    }
                }

                StudyController c = new(_logging_helper, _monDataLayer, _storageDataLayer, source, study_processor);
                harvest.num_records_harvested = c.LoopThroughFiles(opts.harvest_type_id, harvest_id);
            }
            else
            {
                // source type is 'object'
                switch (source.id)
                {
                    case 100135:
                        {
                            object_processor = new PubmedProcessor(_logging_helper);
                            break;
                        }
                }

                ObjectController c = new(_logging_helper, _monDataLayer, _storageDataLayer, source, object_processor);
                harvest.num_records_harvested = c.LoopThroughFiles(opts.harvest_type_id, harvest_id);
            }

            harvest.time_ended = DateTime.Now;
            _monDataLayer.StoreHarvestEvent(harvest);

            _logging_helper.LogLine("Number of source JSON files: " + harvest.num_records_available.ToString());
            _logging_helper.LogLine("Number of files harvested: " + harvest.num_records_harvested.ToString());
            _logging_helper.LogLine("Harvest event " + harvest_id.ToString() + " ended");
        }
    }
}


