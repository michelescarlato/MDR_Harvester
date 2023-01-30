namespace MDR_Harvester;

public interface ITestingDataLayer
{
    int EstablishExpectedData();
    void TransferTestSDData(Source source);
    IEnumerable<int> ObtainTestSourceIDs();
}

