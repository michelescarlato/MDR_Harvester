namespace MDR_Harvester;

public interface ICredentials
{
    string GetConnectionString(string database_name, int harvest_type_id);
}
