using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MDR_Harvester
{
    public class Credentials : ICredentials
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public Credentials(IConfiguration settings)
        {
            // all asserted as non-null

            Host = settings["host"]!;
            Username = settings["user"]!;
            Password = settings["password"]!;
        }

        public string GetConnectionString(string database_name, int harvest_type_id)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.Host = Host;
            builder.Username = Username;
            builder.Password = Password;
            builder.Database = (harvest_type_id == 3) ? "test" : database_name;
            return builder.ConnectionString;
        }
    }
}
