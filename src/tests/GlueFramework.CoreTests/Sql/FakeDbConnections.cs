using System;
using System.Data;
using System.Data.Common;

namespace GlueFramework.CoreTests.Sql
{
    internal abstract class FakeDbConnectionBase : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "Fake";

        public override string DataSource => "Fake";

        public override string ServerVersion => "0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class FakeSqlServerConnection : FakeDbConnectionBase
    {
    }

    internal sealed class FakeMySqlConnection : FakeDbConnectionBase
    {
    }

    internal sealed class FakeNpgsqlConnection : FakeDbConnectionBase
    {
    }
}
