using GlueFramework.Core.ORM;
using System;

namespace GlueFramework.AuditLogModule.Models
{
    [DataTable("AuditLog")]
    public sealed class AuditLogRecord
    {
        [DBField("Id", isKeyField: true, autogenerate: true)]
        public int Id { get; set; }

        [DBField("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        // Keep property names non-reserved; map to existing DB column names.
        [DBField("ActionName")]
        public string? ActionName { get; set; }

        [DBField("Tenant")]
        public string? Tenant { get; set; }

        [DBField("UserName")]
        public string? UserName { get; set; }

        [DBField("Success")]
        public bool Success { get; set; }

        [DBField("ElapsedMs")]
        public int ElapsedMs { get; set; }

        [DBField("TraceId")]
        public string? TraceId { get; set; }

        [DBField("SpanId")]
        public string? SpanId { get; set; }

        [DBField("CorrelationId")]
        public string? CorrelationId { get; set; }

        [DBField("ArgsJson")]
        public string? ArgsJson { get; set; }

        [DBField("ResultJson")]
        public string? ResultJson { get; set; }

        [DBField("Exception")]
        public string? Exception { get; set; }
    }
}
