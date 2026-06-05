using GlueFramework.AuditLog.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.AuditLogModule.Writers
{
    public sealed class LoggerAuditWriter : IAuditWriter
    {
        private readonly ILogger<LoggerAuditWriter> _logger;

        public LoggerAuditWriter(ILogger<LoggerAuditWriter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task WriteAsync(AuditEvent evt, CancellationToken cancellationToken)
        {
            // This intentionally uses structured logging. Serilog can route by properties later.
            _logger.LogInformation(
                "Audit Action={Action} CorrelationId={CorrelationId} Tenant={Tenant} User={User} Success={Success} ElapsedMs={ElapsedMs} TraceId={TraceId} SpanId={SpanId} Args={Args} Result={Result} Exception={Exception}",
                evt.Action,
                evt.CorrelationId,
                evt.Tenant,
                evt.User,
                evt.Success,
                evt.ElapsedMs,
                evt.TraceId,
                evt.SpanId,
                evt.ArgsJson,
                evt.ResultJson,
                evt.Exception);

            return Task.CompletedTask;
        }
    }
}
