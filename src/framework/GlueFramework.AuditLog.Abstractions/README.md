# Glue Framework Audit Log Abstractions

Provides core abstractions for audit logging in the Glue Framework.

## Key Interfaces

- `IAuditWriter` - Write audit events
- `ICorrelationContext` - Correlation ID management
- `[Audit]` - Attribute for marking methods to be audited

## Usage

```csharp
public class MyService
{
    private readonly IAuditWriter _auditWriter;
    
    public MyService(IAuditWriter auditWriter)
    {
        _auditWriter = auditWriter;
    }
    
    [Audit]
    public async Task DoSomethingAsync()
    {
        // Method execution will be automatically audited
    }
}
```
