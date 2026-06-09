using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.Core.Services
{
    public sealed class InProcEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InProcEventBus> _logger;

        public InProcEventBus(IServiceProvider serviceProvider, ILogger<InProcEventBus> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task PublishAfterCommitAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        {
            return PublishAfterCommitAsync(evt, executeInProcHandlers: true, cancellationToken);
        }

        public Task PublishAfterCommitAsync<TEvent>(TEvent evt, bool executeInProcHandlers, CancellationToken cancellationToken = default)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            if (!executeInProcHandlers)
                return Task.CompletedTask;

            TransactionScopeContext.EnqueueAfterCommit(() =>
            {
                // After commit is typically executed after the request is effectively complete.
                // Do not couple best-effort handlers to the request cancellation token.
                using (ExecutionContext.SuppressFlow())
                {
                    _ = Task.Run(async () => await PublishNowBestEffortAsync(evt, CancellationToken.None).ConfigureAwait(false));
                }
            });

            return Task.CompletedTask;
        }

        public Task PublishNowBestEffortAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        {
            // Best-effort publishing should not be canceled by an HTTP request abort.
            // If the caller truly needs cancellation semantics, use PublishNowRequiredAsync.
            return PublishInternalAsync(evt, CancellationToken.None, required: false);
        }

        public Task PublishNowRequiredAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        {
            return PublishInternalAsync(evt, cancellationToken, required: true);
        }

        private async Task PublishInternalAsync<TEvent>(TEvent evt, CancellationToken cancellationToken, bool required)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ToArray();
            if (handlers.Length == 0)
                return;

            var messageId = EventDispatchContext.MessageId;
            IInboxStore? inbox = null;
            if (messageId.HasValue)
                inbox = scope.ServiceProvider.GetService<IInboxStore>();

            List<Exception>? exceptions = null;

            foreach (var h in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (messageId.HasValue && inbox != null)
                    {
                        var handlerName = h.GetType().FullName ?? h.GetType().Name;
                        var canHandle = await inbox.TryBeginHandleAsync(messageId.Value, handlerName, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
                        if (!canHandle)
                            continue;
                    }

                    await h.HandleAsync(evt, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler failed. Event={EventType}, Handler={HandlerType}, Required={Required}", typeof(TEvent).FullName, h.GetType().FullName, required);

                    if (required)
                        throw;

                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }
        }
    }
}
