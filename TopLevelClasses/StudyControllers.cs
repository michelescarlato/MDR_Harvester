using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;

namespace MDR_Harvester
{
    public class StudyController
    {
        private readonly ILoggingHelper _logger;
        private readonly IMonDataLayer _mon_data_layer;
        private readonly IStorageDataLayer _storage_repo;
        private readonly IStudyProcessor _processor;
        private readonly ISource _source;

        public StudyController(ILoggingHelper logger, IMonDataLayer mon_data_layer, IStorageDataLayer storage_repo,
                              ISource source, IStudyProcessor processor)
        {
            _logger = logger;
            _mon_data_layer = mon_data_layer;
            _storage_repo = storage_repo;
            _processor = processor;
            _source = source;
        }

        public int? LoopThroughFiles(int harvest_type_id, int harvest_id)
        {
            // Loop through the available records a chunk at a time (may be 1 for smaller record sources)
            // First get the total number of records in the system for this source
            // Set up the outer limit and get the relevant records for each pass.

            int source_id = _source.id.HasValue ? (int)_source.id : 0;
            int total_amount = _mon_data_layer.FetchFileRecordsCount(source_id, _source.source_type!, harvest_type_id);
            int chunk = _source.harvest_chunk.HasValue ? (int)_source.harvest_chunk : 0;
            int k = 0;
            for (int m = 0; m < total_amount; m += chunk)
            {
                if (k >= 2000) break; // for testing...

                IEnumerable<StudyFileRecord> file_list = _mon_data_layer
                        .FetchStudyFileRecordsByOffset(source_id, m, chunk, harvest_type_id);

                int n = 0; string? filePath;
                foreach (StudyFileRecord rec in file_list)
                {
                    if (k > 2000) break; // for testing...

                    n++; k++;
                    filePath = rec.local_path;
                    if (File.Exists(filePath))
                    {
                        string jsonString = File.ReadAllText(filePath);
                        Study? s = _processor.ProcessData(jsonString, rec.last_downloaded);

                        if (s is not null)
                        {
                            // store the data in the database			
                            _storage_repo.StoreFullStudy(s, _source);

                            // update file record with last processed datetime
                            // (if not in test mode)
                            if (harvest_type_id != 3)
                            {
                                _mon_data_layer.UpdateFileRecLastHarvested(rec.id, _source.source_type, harvest_id);
                            }
                        }
                    }

                    if (k % chunk == 0) _logger.LogLine("Records harvested: " + k.ToString());
                }
            }

            return k;
        }
    }
}
