using GlueFramework.Core.Abstractions;
using System.Text.Json;
using Demo.DDD.OrchardCore.Infrastructure;

namespace Demo.DDD.OrchardCore.Application.EventHandlers
{
    public sealed class OutboxAppendHandler<TEvent> : IEventHandler<TEvent>
    {
        private readonly InMemoryOutbox _outbox;

        public OutboxAppendHandler(InMemoryOutbox outbox)
        {
            _outbox = outbox;
        }

        public Task HandleAsync(TEvent evt, CancellationToken cancellationToken = default)
        {
            var type = typeof(TEvent).FullName ?? typeof(TEvent).Name;
            var payload = JsonSerializer.Serialize(evt);
            _outbox.Append(type, payload);
            return Task.CompletedTask;
        }
    }
}
