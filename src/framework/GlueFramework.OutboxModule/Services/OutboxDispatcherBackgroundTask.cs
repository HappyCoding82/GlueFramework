using GlueFramework.OutboxModule.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.BackgroundTasks;

namespace GlueFramework.OutboxModule.Services
{
    public sealed class OutboxDispatcherBackgroundTask : IBackgroundTask
    {
        private readonly IOptions<OutboxOptions> _configOptions;
        private readonly ILogger<OutboxDispatcherBackgroundTask> _logger;

        public OutboxDispatcherBackgroundTask(
            IOptions<OutboxOptions> configOptions,
            ILogger<OutboxDispatcherBackgroundTask> logger)
        {
            _configOptions = configOptions;
            _logger = logger;
        }

        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var opt = scope.ServiceProvider.GetRequiredService<IOptions<OutboxOptions>>();
                if (!opt.Value.Enabled)
                    return;

                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatchService>();
                await dispatcher.ExecuteOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox background task error");
            }
        }
    }
}
