using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tierdrop.Outbox.Abstractions
{
    public interface IOutboxStore
    {
        Task<Guid> EnqueueAsync(string type, string payload, DateTimeOffset occurredUtc, CancellationToken cancellationToken = default);

        Task<List<OutboxMessageRecord>> GetPendingAsync(int batchSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

        Task<bool> TryMarkProcessingAsync(Guid messageId, DateTimeOffset nowUtc, DateTimeOffset lockedUntilUtc, CancellationToken cancellationToken = default);

        Task MarkSucceededAsync(Guid messageId, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

        Task MarkFailedAsync(Guid messageId, DateTimeOffset nowUtc, string error, int nextRetrySeconds, CancellationToken cancellationToken = default);

        Task<List<OutboxMessageRecord>> QueryAsync(int take, string? status, CancellationToken cancellationToken = default);
    }
}
