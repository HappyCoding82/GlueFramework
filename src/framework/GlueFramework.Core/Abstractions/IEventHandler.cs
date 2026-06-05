using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.Core.Abstractions
{
    public interface IEventHandler<in TEvent>
    {
        Task HandleAsync(TEvent evt, CancellationToken cancellationToken = default);
    }
}
