using GlueFramework.Core.Abstractions;
using System.Data.Common;
using System.Data;

namespace GlueFramework.Core.UOW
{
    public readonly struct DbSessionScope : IDbSession, IDisposable
    {
        private readonly DbConnection? _ownedConnection;

        public DbSessionScope(IDbConnection connection, IDbTransaction? transaction, DbConnection? ownedConnection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction;
            _ownedConnection = ownedConnection;
        }

        public IDbConnection Connection { get; }

        public IDbTransaction? Transaction { get; }

        public void Dispose()
        {
            if (_ownedConnection != null)
            {
                _ownedConnection.Close();
                _ownedConnection.Dispose();
            }
        }
    }
}
