namespace MDR_Harvester;

public class StudyController
{
    private readonly ILoggingHelper _loggingHelper;
    private readonly IMonDataLayer _monDataLayer;
    private readonly IStorageDataLayer _storageDataLayer;
    private readonly IStudyProcessor _processor;
    private readonly Source _source;
    private readonly Options _opts;
    
    public StudyController(ILoggingHelper loggingHelper, IMonDataLayer monDataLayer, IStorageDataLayer storageDataLayer,
                          Source source, Options opts, IStudyProcessor processor)
    {
        _loggingHelper = loggingHelper;
        _monDataLayer = monDataLayer;
        _storageDataLayer = storageDataLayer;
        _processor = processor;
        _source = source;
        _opts = opts;
    }

    public int? LoopThroughFiles(int harvestTypeId, int harvestId)
    {
        // Loop through the available records a chunk at a time (may be 1 for smaller record sources)
        // First get the total number of records in the system for this source
        // Set up the outer limit and get the relevant records for each pass.

        int skip_recent_days = _opts.SkipRecentDays ?? 0;
        int amount_to_fetch = _monDataLayer.FetchFileRecordsCount(harvestTypeId, skip_recent_days);
        int chunk = _source.harvest_chunk ?? 0;
        int k = 0;

        // total_amount = 1; // for testing
        
        for (int m = 0; m < amount_to_fetch; m += chunk)    // 
        {
            //if (k >= 40000) break; // for testing...

            IEnumerable<StudyFileRecord> file_list = _monDataLayer
                    .FetchStudyFileRecordsByOffset(m, chunk, harvestTypeId, skip_recent_days);

            foreach (StudyFileRecord rec in file_list)
            {
                //if (k > 5000) break; // for testing...

                k++;
                string? filePath = rec.local_path;
                if (filePath is not null && File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    Study? s = _processor.ProcessData(jsonString, rec.last_downloaded, _loggingHelper);

                    if (s is not null)
                    {
                        // store the data in the database			
                        _storageDataLayer.StoreFullStudy(s, _source);

                        // update file record with last processed datetime
                        // (if not in test mode)
                        if (harvestTypeId != 3)
                        {
                            _monDataLayer.UpdateFileRecLastHarvested(rec.id, _source.source_type!, harvestId);
                        }
                    }
                }
                if (k % 100 == 0) _loggingHelper.LogLine("Records harvested: " + k.ToString());
                //if (k % chunk == 0) _loggingHelper.LogLine("Records harvested: " + k.ToString());
            }
        }

        return k;
    }
}

