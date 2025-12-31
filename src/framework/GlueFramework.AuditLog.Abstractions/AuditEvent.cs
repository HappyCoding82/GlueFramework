using System;

namespace GlueFramework.AuditLog.Abstractions
{
    public sealed class AuditEvent
    {
        public string Action { get; set; } = string.Empty;

        public string? CorrelationId { get; set; }

        public string? Tenant { get; set; }

        public string? User { get; set; }

        public string? TraceId { get; set; }

        public string? SpanId { get; set; }

        public DateTimeOffset OccurredUtc { get; set; } = DateTimeOffset.UtcNow;

        public bool? Success { get; set; }

        public long? ElapsedMs { get; set; }

        public string? ArgsJson { get; set; }

        public string? ResultJson { get; set; }

        public string? Exception { get; set; }
    }
}
