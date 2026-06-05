namespace GlueFramework.AuditLog.Abstractions
{
    public interface ICorrelationContext
    {
        string? CorrelationId { get; }

        string? Tenant { get; }

        string? User { get; }
    }
}
