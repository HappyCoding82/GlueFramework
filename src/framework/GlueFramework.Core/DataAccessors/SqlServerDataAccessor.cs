using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Data.SqlClient;

namespace GlueFramework.Core.DataAccessors
{
    public class SqlServerDataAccessor : Abstractions.IDbConnectionAccessor
    {
        private IConfiguration? _configuration = default(IConfiguration);

        public SqlServerDataAccessor(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        private SqlServerDataAccessor(string connStr)
        {
            _connStr = connStr;
        }

        private string _connStr = string.Empty;
        public static Abstractions.IDbConnectionAccessor CreateDataAccessor(string connectionString)
        {
            return new SqlServerDataAccessor(connectionString);
        }
       
        public DbConnection CreateConnection()
        {
            var dbConn = new SqlConnection(string.IsNullOrEmpty(_connStr) ? _configuration.GetConnectionString("DefaultConnection") : _connStr);
            //dbConn.Open();
            return dbConn;
        }
    }
}
