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
    LoggingHelper? _logging_helper;
    private readonly IMonitorDataLayer _mon_repo;
    private readonly IStorageDataLayer _storage_repo;
    private readonly ITestingDataLayer _test_repo;

    public Harvester(IMonitorDataLayer mon_repo, IStorageDataLayer storage_repo, 
                     ITestingDataLayer test_repo)
    {
        _mon_repo = mon_repo;
        _storage_repo = storage_repo;
        _test_repo = test_repo;
    }

    public int Run(Options opts)
    {
        try
        {
            foreach (int source_id in opts.source_ids)
            {
                // Obtain source details, augment with connection string for this database

                ISource source = _mon_repo.FetchSourceParameters(source_id);
                Credentials creds = _mon_repo.Credentials;
                source.db_conn = creds.GetConnectionString(source.database_name, opts.harvest_type_id);

                // establish and begin the logger helper for this harvest

                _logging_helper.OpenLogFile(source.database_name);
                _logging_helper.LogCommandLineParameters(opts);
                _logging_helper.LogHeader("STARTING HARVESTER");
                _logging_helper.LogStudyHeader(opts, "For source: " + source.id + ": " + source.database_name);

                // call the main routine to do the harvesting, if not just a context data update

                if (!opts.org_update_only)
                {
                    HarvestData(source, opts, creds, _logging_helper);
                }

                // relinquish control on log file to enable later stages to re-usae it

                _logging_helper.SwitchLog();  

                // called for all options and source types

                //UpdateContextData(source, opts, creds, _logging_helper);
                //UpdateHashData(source, opts, _logging_helper);
                TidyAndWriteStatistics(source, opts, _logging_helper);
                _logging_helper = null;

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



    private void HarvestData(ISource source, Options opts, Credentials creds, LoggingHelper logging_helper)
    {

        if (!opts.org_update_only)
        {
            // Bulk of the harvesting process can be skipped if this run is just for updating 
            // tables with context values. 

            if (source.source_type == "test")
            {
                // Set up expected data for later processing.
                // This is data derived from manual inspection of files and requires
                // a very different method, using stored procedures in the test db
                _test_repo.EstablishExpectedData();
            }
            else
            {
                // Otherwise...
                // construct the sd tables. (Some sources may be data objects only.)

                _logging_helper.LogHeader("Recreate database tables");
                SchemaBuilder sdb = new SchemaBuilder(source, logging_helper);
                sdb.RecreateTables();

                // Construct the harvest_event record.

                _logging_helper.LogHeader("Process data");
                int harvest_id = _mon_repo.GetNextHarvestEventId();
                HarvestEvent harvest = new HarvestEvent(harvest_id, source.id, opts.harvest_type_id);
                _logging_helper.LogLine("Harvest event " + harvest_id.ToString() + " began");

                // Harvest the data from the local XML files
                IStudyProcessor study_processor = null;
                IObjectProcessor object_processor = null;
                harvest.num_records_available = _mon_repo.FetchFullFileCount(source.id, source.source_type, opts.harvest_type_id);

                if (source.source_type == "study")
                {
                    if (source.uses_who_harvest)
                    {
                        study_processor = new WHOProcessor(_mon_repo, logging_helper);
                    }
                    else
                    {
                        switch (source.id)
                        {
                            case 101900:
                                {
                                    study_processor = new BioLinccProcessor(_mon_repo, logging_helper);
                                    break;
                                }
                            case 101901:
                                {
                                    study_processor = new YodaProcessor(_mon_repo, logging_helper);
                                    break;
                                }
                            case 100120:
                                {
                                    study_processor = new CTGProcessor(_mon_repo, logging_helper);
                                    break;
                                }
                            case 100123:
                                {
                                    study_processor = new EUCTRProcessor(_mon_repo, logging_helper);
                                    break;
                                }
                            case 100126:
                                {
                                    study_processor = new ISRCTNProcessor(_mon_repo, logging_helper);
                                    break;
                                }
                        }
                    }

                    StudyController c = new StudyController(logging_helper, _mon_repo, _storage_repo, source, study_processor);
                    harvest.num_records_harvested = c.LoopThroughFiles(opts.harvest_type_id, harvest_id);
                }
                else
                {
                    // source type is 'object'
                    switch (source.id)
                    {
                        case 100135:
                            {
                                object_processor = new PubmedProcessor(_mon_repo, logging_helper);
                                break;
                            }
                    }

                    ObjectController c = new ObjectController(logging_helper, _mon_repo, _storage_repo, source, object_processor);
                    harvest.num_records_harvested = c.LoopThroughFiles(opts.harvest_type_id, harvest_id);
                }

                harvest.time_ended = DateTime.Now;
                _mon_repo.StoreHarvestEvent(harvest);

                logging_helper.LogLine("Number of source XML files: " + harvest.num_records_available.ToString());
                logging_helper.LogLine("Number of files harvested: " + harvest.num_records_harvested.ToString());
                logging_helper.LogLine("Harvest event " + harvest_id.ToString() + " ended");
            }
        }
    }

    /*
    private void UpdateContextData(ISource source, Options opts, Credentials creds, LoggingHelper logging_helper)
    { 
        // -------------------------------------------------------------------
        // MAKES USE OF SEPARATE 'CONTEXT' PROJECT (Same Solution, not DLL) 
        // -------------------------------------------------------------------

         ContextDataManager.Source context_source = new ContextDataManager.Source(source.id, source.source_type, source.database_name, source.db_conn,
                                                   source.has_study_tables, source.has_study_topics, source.has_study_contributors,
                                                   source.has_study_countries, source.has_study_locations);
         ContextDataManager.Credentials context_creds = new ContextDataManager.Credentials(creds.Host, creds.Username, creds.Password);
        
         ContextMain context_main = new ContextMain(context_creds, context_source, logging_helper.LogFilePath);
         context_main.UpdateDataFromContext();
    }


    private void UpdateHashData(ISource source, Options opts, LoggingHelper logging_helper)
    {
        // -------------------------------------------------------------------
        // MAKES USE OF SEPARATE 'HASH' PROJECT (Same Solution, not DLL) 
        // -------------------------------------------------------------------

        // Note the hashes can only be done after all the data is complete, including 
        // the organisation and topic codes and names derived above

        HashDataLibrary.Source hash_source = new HashDataLibrary.Source(source.id, source.source_type, source.database_name, source.db_conn,
                  source.has_study_tables, source.has_study_topics, source.has_study_features,
                  source.has_study_contributors, source.has_study_references, source.has_study_relationships,
                  source.has_study_links, source.has_study_ipd_available, source.has_study_countries,
                  source.has_study_locations, source.has_object_datasets,
                  source.has_object_dates, source.has_object_rights, source.has_object_relationships,
                  source.has_object_pubmed_set);

        HashMain hash_main = new HashMain(hash_source, logging_helper.LogFilePath);
        hash_main.HashData();
    }
    */

    private void TidyAndWriteStatistics(ISource source, Options opts, LoggingHelper logging_helper)
    {
        // If harvesting test data it needs to be transferred  
        // to the sdcomp schema for safekeeping and further processing
        // If a normal harvest from a full source statistics should be produced.
        // If the harvest was of the manual 'expected' data do neither.

        logging_helper.Reattach();

        if (source.source_type != "test")
        {
            // if not loadingt the 'expected' test data

            if (opts.harvest_type_id == 3)
            {
                // transfer sd data to test composite data store for later comparison
                // otherwise it will be overwritten by the next harvest of sd data

                _test_repo.TransferTestSDData(source);
            }
            else
            {
                // summarise results by providing stats on the sd tables
                logging_helper.LogTableStatistics(source, "sd");
            }
        }

        logging_helper.CloseLog();

    }

}


