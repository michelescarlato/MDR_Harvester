namespace MDR_Harvester;

public interface IStorageDataLayer
{
    void StoreFullStudy(Study s, Source source);
    void StoreFullObject(FullDataObject b, Source source);
    ObjectTypeDetails? FetchDocTypeDetails(string biolincc_db_conn, string docName);
}
