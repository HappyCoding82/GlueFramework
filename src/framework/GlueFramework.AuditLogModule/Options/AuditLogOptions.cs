namespace GlueFramework.AuditLogModule.Options
{
    public sealed class AuditLogOptions
    {
        public bool Enabled { get; set; } = true;

        public AuditWriterMode WriterMode { get; set; } = AuditWriterMode.Database;

        public bool IgnoreWriteErrors { get; set; } = true;

        public DalStepMode DalStepMode { get; set; } = DalStepMode.FailOnly;

        public string? CorrelationHeaderName { get; set; } = "X-Correlation-Id";

        public string? UserHeaderName { get; set; } = "X-User";
     
        public string? TenantHeaderName { get; set; } = "X-Tenant";
    }

    public enum AuditWriterMode
    {
        Logger = 0,
        Database = 1
    }

    public enum DalStepMode
    {
        Off = 0,
        FailOnly = 1,
        Always = 2
    }
}
