namespace MDR_Harvester;

public interface IMonDataLayer
{
    string GetConnectionString(string database_name, int harvest_type_id);
    Source FetchSourceParameters(int source_id);
    
    int FetchFileRecordsCount(int source_id, string source_type, int harvest_type_id = 1, int days_ago = 0);
    int FetchFullFileCount(int source_id, string source_type, int harvest_type_id);

    IEnumerable<ObjectFileRecord> FetchObjectFileRecordsByOffset(int source_id, int offset_num, int amount, 
        int harvest_type_id = 1, int days_ago = 0);
    IEnumerable<StudyFileRecord> FetchStudyFileRecordsByOffset(int source_id, int offset_num, 
        int amount, int harvest_type_id = 1, int days_ago = 0);
    
    int GetNextHarvestEventId();
    bool SourceIdPresent(int source_id);
    int StoreHarvestEvent(HarvestEvent harvest);
    void UpdateFileRecLastHarvested(int? id, string source_type, int last_harvest_id);
}
