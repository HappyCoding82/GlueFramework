using System;
using System.Data;
using System.Data.Common;

namespace GlueFramework.Core.Diagnostics
{
    public static class DbConnectionUnwrapper
    {
        public static IDbConnection Unwrap(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            while (connection is ProfilingDbConnection profiling)
            {
                connection = profiling.InnerConnection;
            }

            return connection;
        }

        public static DbConnection Unwrap(DbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            return (DbConnection)Unwrap((IDbConnection)connection);
        }
    }
}
