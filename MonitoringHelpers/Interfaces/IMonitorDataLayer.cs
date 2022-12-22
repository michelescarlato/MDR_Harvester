using System;
using System.Collections.Generic;

namespace DataHarvester
{
    public interface IMonitorDataLayer
    {
        Credentials Credentials { get; }

        int FetchFileRecordsCount(int source_id, string source_type, int harvest_type_id = 1, DateTime? cutoff_date = null);
        int FetchFullFileCount(int source_id, string source_type, int harvest_type_id);
        ObjectFileRecord FetchObjectFileRecord(string sd_id, int source_id, string source_type);
        IEnumerable<ObjectFileRecord> FetchObjectFileRecords(int source_id, int harvest_type_id = 1);
        IEnumerable<ObjectFileRecord> FetchObjectFileRecordsByOffset(int source_id, int offset_num, int amount, int harvest_type_id = 1);
        Source FetchSourceParameters(int source_id);
        StudyFileRecord FetchStudyFileRecord(string sd_id, int source_id, string source_type);
        IEnumerable<StudyFileRecord> FetchStudyFileRecords(int source_id, int harvest_type_id = 1);
        IEnumerable<StudyFileRecord> FetchStudyFileRecordsByOffset(int source_id, int offset_num, int amount, int harvest_type_id = 1);
        int GetNextHarvestEventId();
        string lang_3_to_2(string lang_code_3);
        bool SourceIdPresent(int source_id);
        int StoreHarvestEvent(HarvestEvent harvest);
        void UpdateFileRecLastHarvested(int id, string source_type, int last_harvest_id);
    }
}