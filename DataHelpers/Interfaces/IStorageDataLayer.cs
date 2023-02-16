namespace MDR_Harvester;

public interface IStorageDataLayer
{
    void StoreFullStudy(Study s, Source source);
    void StoreFullObject(FullDataObject b, Source source);
}
