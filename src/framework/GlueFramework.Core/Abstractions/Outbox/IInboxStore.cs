using System;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.Core.Abstractions.Outbox
{
    public interface IInboxStore
    {
        Task<bool> TryBeginHandleAsync(Guid messageId, string handler, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

        Task<int> CleanupAsync(DateTimeOffset olderThanUtc, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
