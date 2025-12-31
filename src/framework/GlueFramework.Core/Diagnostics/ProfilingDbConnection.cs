using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Data.Common;

namespace GlueFramework.Core.Diagnostics
{
    public sealed class ProfilingDbConnection : DbConnection
    {
        private readonly DbConnection _inner;
        private readonly IOptions<SlowSqlOptions> _options;
        private readonly ILogger _logger;

        public DbConnection InnerConnection => _inner;

        public ProfilingDbConnection(DbConnection inner, IOptions<SlowSqlOptions> options, ILogger logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => _inner.BeginTransaction(isolationLevel);

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        public override void Close() => _inner.Close();

        public override string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        protected override DbCommand CreateDbCommand()
        {
            var cmd = _inner.CreateCommand();
            return new ProfilingDbCommand(cmd, _options, _logger);
        }

        public override string Database => _inner.Database;

        public override void Open() => _inner.Open();

        public override string DataSource => _inner.DataSource;

        public override string ServerVersion => _inner.ServerVersion;

        public override ConnectionState State => _inner.State;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
