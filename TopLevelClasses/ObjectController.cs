using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MDR_Harvester
{
    public class ObjectController
    {
        private readonly ILoggingHelper _loggingHelper;
        private readonly IMonDataLayer _monDataLayer;
        private readonly IStorageDataLayer _storageDataLayer;
        private readonly IObjectProcessor _processor;
        private readonly ISource _source;

        public ObjectController(ILoggingHelper loggingHelper, IMonDataLayer monDataLayer, IStorageDataLayer storageDataLayer,
                                ISource source, IObjectProcessor processor)
        {
            _loggingHelper = loggingHelper;
            _monDataLayer = monDataLayer;
            _storageDataLayer = storageDataLayer;
            _processor = processor;
            _source = source;
        }

        public int? LoopThroughFiles(int harvest_type_id, int harvest_id)
        {
            // Loop through the available records a chunk at a time (may be 1 for smaller record sources)
            // First get the total number of records in the system for this source
            // Set up the outer limit and get the relevant records for each pass.

            int source_id = _source.id.HasValue ? (int)_source.id : 0; 
            int total_amount = _monDataLayer.FetchFileRecordsCount(source_id, _source.source_type!, harvest_type_id);
            int chunk = _source.harvest_chunk.HasValue ? (int)_source.harvest_chunk : 0;
            int k = 0;
            for (int m = 0; m < total_amount; m += chunk)
            {
                // if (k > 2000) break; // for testing...

                IEnumerable<ObjectFileRecord> file_list = _monDataLayer
                        .FetchObjectFileRecordsByOffset(source_id, m, chunk, harvest_type_id);

                int n = 0; string? filePath;
                foreach (ObjectFileRecord rec in file_list)
                {
                    // if (k > 50) break; // for testing...

                    n++; k++;
                    filePath = rec.local_path;
                    if (File.Exists(filePath))
                    {
                        string jsonString = File.ReadAllText(filePath);
                        FullDataObject? s = _processor.ProcessData(jsonString, rec.last_downloaded);

                        if (s is not null)
                        {
                            // store the data in the database			
                            _storageDataLayer.StoreFullObject(s, _source);

                            // update file record with last processed datetime
                            // (if not in test mode)
                            if (harvest_type_id != 3)
                            {
                                _monDataLayer.UpdateFileRecLastHarvested(rec.id, _source.source_type!, harvest_id);
                            }
                        }
                    }

                    if (k % chunk == 0) _loggingHelper.LogLine("Records harvested: " + k.ToString());
                }

            }
            return k;
        }
    }
}
