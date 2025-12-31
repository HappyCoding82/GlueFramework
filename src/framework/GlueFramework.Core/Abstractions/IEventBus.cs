using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.Core.Abstractions
{
    public interface IEventBus
    {
        Task PublishAfterCommitAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default);

        Task PublishNowBestEffortAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default);

        Task PublishNowRequiredAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default);
    }
}
