namespace MDR_Harvester;

public interface ITestDataLayer
{
    int EstablishExpectedData();
    void TransferTestSDData(Source source);
    IEnumerable<int> ObtainTestSourceIDs();
}

