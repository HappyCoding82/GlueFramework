using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.AuditLog.Abstractions
{
    public interface IAuditWriter
    {
        Task WriteAsync(AuditEvent evt, CancellationToken cancellationToken);
    }
}
