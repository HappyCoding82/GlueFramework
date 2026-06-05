using GlueFramework.AuditLog.Abstractions;
using GlueFramework.AuditLogModule.Models;
using GlueFramework.AuditLogModule.Options;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.UOW;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GlueFramework.AuditLogModule.Writers
{
    public sealed class DbAuditWriter : IAuditWriter
    {
        private readonly IDbConnectionAccessor _dbConnectionAccessor;
        private readonly IDataTablePrefixProvider _tablePrefixProvider;
        private readonly IOptions<AuditLogOptions> _options;
        private readonly ILogger<DbAuditWriter> _logger;

        public DbAuditWriter(
            IDbConnectionAccessor dbConnectionAccessor,
            IOptions<AuditLogOptions> options,
            ILogger<DbAuditWriter> logger,
            IDataTablePrefixProvider tablePrefixProvider)
        {
            _dbConnectionAccessor = dbConnectionAccessor ?? throw new ArgumentNullException(nameof(dbConnectionAccessor));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tablePrefixProvider = tablePrefixProvider ?? throw new ArgumentNullException(nameof(tablePrefixProvider));
        }

        public async Task WriteAsync(AuditEvent evt, CancellationToken cancellationToken)
        {
            var opt = _options.Value;
            if (!opt.Enabled)
                return;

            try
            {
                await using var conn = _dbConnectionAccessor.CreateConnection();

                var repo = new Repository<AuditLogRecord>(conn, _tablePrefixProvider);
                await repo.InsertAsync(new AuditLogRecord
                {
                    CreatedUtc = evt.OccurredUtc.UtcDateTime,
                    ActionName = evt.Action,
                    Tenant = evt.Tenant,
                    UserName = evt.User,
                    Success = evt.Success ?? false,
                    ElapsedMs = (int)(evt.ElapsedMs ?? 0),
                    TraceId = evt.TraceId,
                    SpanId = evt.SpanId,
                    CorrelationId = evt.CorrelationId,
                    ArgsJson = evt.ArgsJson,
                    ResultJson = evt.ResultJson,
                    Exception = evt.Exception
                });
            }
            catch (Exception ex)
            {
                if (!opt.IgnoreWriteErrors)
                    throw;

                _logger.LogWarning(ex, "AuditLog DB write failed. Action={Action} Tenant={Tenant} User={User}", evt.Action, evt.Tenant, evt.User);
            }
        }
    }
}
