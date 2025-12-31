using Npgsql;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace OrchardCore.MultiTenant.Migration.Helpers
{
    public static class TenantProviderHelper
    {
        public static string NormalizeProvider(string provider) => provider switch
        {
            "SqlConnection" or "SqlServer" => "SqlServer",
            "Postgres" or "PostgreSQL" => "PostgreSQL",
            _ => provider
        };

        public static DbConnection CreateConnection(string provider, string conn) => provider switch
        {
            "SqlServer" => new SqlConnection(conn),
            "PostgreSQL" => new NpgsqlConnection(conn),
            _ => throw new NotSupportedException(provider)
        };
    }

}
