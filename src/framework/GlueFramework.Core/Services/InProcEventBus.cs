using GlueFramework.Core.Abstractions;
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
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            TransactionScopeContext.EnqueueAfterCommit(() =>
            {
                _ = PublishNowBestEffortAsync(evt, cancellationToken);
            });

            return Task.CompletedTask;
        }

        public Task PublishNowBestEffortAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        {
            return PublishInternalAsync(evt, cancellationToken, required: false);
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

            List<Exception>? exceptions = null;

            foreach (var h in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
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
