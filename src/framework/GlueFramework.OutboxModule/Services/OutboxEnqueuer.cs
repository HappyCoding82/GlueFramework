using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GlueFramework.Core.Abstractions.Outbox;

namespace GlueFramework.OutboxModule.Services
{
    public interface IOutboxEnqueuer
    {
        Task<Guid> EnqueueAsync<T>(T evt, CancellationToken cancellationToken = default);
    }

    public sealed class OutboxEnqueuer : IOutboxEnqueuer
    {
        private readonly IOutboxStore _store;

        public OutboxEnqueuer(IOutboxStore store)
        {
            _store = store;
        }

        public Task<Guid> EnqueueAsync<T>(T evt, CancellationToken cancellationToken = default)
        {
            var type = evt?.GetType().AssemblyQualifiedName ?? typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? "Unknown";
            var payload = JsonSerializer.Serialize(evt, evt?.GetType() ?? typeof(T), OutboxJson.Options);
            return _store.EnqueueAsync(type, payload, DateTimeOffset.UtcNow, cancellationToken);
        }
    }
}
