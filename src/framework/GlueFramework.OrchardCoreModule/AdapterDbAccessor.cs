using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace GlueFramework.OrchardCoreModule
{
    public class AdapterDbAccessor : IDbConnectionAccessor
    {
        private readonly OrchardCore.Data.IDbConnectionAccessor _orchardCoreDBAccessor;
        private readonly IOptions<SlowSqlOptions> _slowSqlOptions;
        private readonly ILogger<AdapterDbAccessor> _logger;

        public AdapterDbAccessor(
            OrchardCore.Data.IDbConnectionAccessor orchardCoreDBAccessor,
            IOptions<SlowSqlOptions> slowSqlOptions,
            ILogger<AdapterDbAccessor> logger)
        {
            _orchardCoreDBAccessor = orchardCoreDBAccessor;
            _slowSqlOptions = slowSqlOptions;
            _logger = logger;
        }

        public DbConnection CreateConnection()
        {
            var conn = _orchardCoreDBAccessor.CreateConnection();
            if (_slowSqlOptions.Value.Enabled)
            {
                return new ProfilingDbConnection(conn, _slowSqlOptions, _logger);
            }

            return conn;
        }
    }
}
