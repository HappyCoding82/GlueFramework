using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Settings;
using GlueFramework.OutboxModule.Options;
using GlueFramework.OutboxModule.Settings;

namespace GlueFramework.OutboxModule.Services
{
    public sealed class OutboxDispatcherHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<OutboxOptions> _configOptions;
        private readonly ILogger<OutboxDispatcherHostedService> _logger;

        public OutboxDispatcherHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxOptions> configOptions,
            ILogger<OutboxDispatcherHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _configOptions = configOptions;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
                    var opt = await LoadMergedOptionsAsync(siteService);
                    if (!opt.Enabled)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatchService>();
                    await dispatcher.ExecuteOnceAsync(stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, opt.DispatchIntervalSeconds)), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox dispatcher loop error");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task<OutboxSettings> LoadMergedOptionsAsync(ISiteService siteService)
        {
            var merged = new OutboxSettings
            {
                Enabled = _configOptions.Value.Enabled,
                AutoEnqueueIntegrationEvents = _configOptions.Value.AutoEnqueueIntegrationEvents,
                DispatchIntervalSeconds = _configOptions.Value.DispatchIntervalSeconds,
                BatchSize = _configOptions.Value.BatchSize,
                InboxRetentionDays = _configOptions.Value.InboxRetentionDays,
                EnableInboxCleanup = _configOptions.Value.EnableInboxCleanup,
            };

            var site = await siteService.LoadSiteSettingsAsync();
            var settings = site.As<OutboxSettings>();
            if (settings != null)
            {
                merged.Enabled = settings.Enabled;
                merged.AutoEnqueueIntegrationEvents = settings.AutoEnqueueIntegrationEvents;
                merged.DispatchIntervalSeconds = settings.DispatchIntervalSeconds;
                merged.BatchSize = settings.BatchSize;
                merged.InboxRetentionDays = settings.InboxRetentionDays;
                merged.EnableInboxCleanup = settings.EnableInboxCleanup;
            }

            return merged;
        }

        private static int BackoffSeconds(int tryCount)
        {
            if (tryCount <= 1) return 5;
            if (tryCount == 2) return 15;
            if (tryCount == 3) return 60;
            if (tryCount == 4) return 180;
            return 600;
        }
    }
}
