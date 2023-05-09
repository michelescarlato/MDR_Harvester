namespace MDR_Harvester;

public interface ICredentials
{
    string GetConnectionString(string database_name);
}
