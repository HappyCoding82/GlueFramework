using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.OutboxModule.Options;
using GlueFramework.OutboxModule.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.BackgroundTasks;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
                var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                var opt = await LoadMergedOptionsAsync(siteService);
                if (!opt.Enabled)
                    return;

                await DispatchOnceAsync(outbox, eventBus, opt, cancellationToken);

                if (opt.EnableInboxCleanup)
                    await CleanupInboxAsync(inbox, opt, cancellationToken);
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

        private static async Task DispatchOnceAsync(IOutboxStore outbox, IEventBus eventBus, OutboxSettings opt, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var batch = await outbox.GetPendingAsync(Math.Max(1, opt.BatchSize), now, cancellationToken);

            foreach (var msg in batch)
            {
                var locked = await outbox.TryMarkProcessingAsync(msg.MessageId, now, now.AddMinutes(2), cancellationToken);
                if (!locked)
                    continue;

                try
                {
                    var type = Type.GetType(msg.Type);
                    if (type == null)
                        throw new InvalidOperationException($"Cannot resolve event type: {msg.Type}");

                    var evt = JsonSerializer.Deserialize(msg.Payload, type, OutboxJson.Options);
                    if (evt == null)
                        throw new InvalidOperationException($"Cannot deserialize event payload. Type={msg.Type}");

                    await eventBus.PublishNowRequiredAsync((dynamic)evt, cancellationToken);
                    await outbox.MarkSucceededAsync(msg.MessageId, DateTimeOffset.UtcNow, cancellationToken);
                }
                catch (Exception ex)
                {
                    var nextRetrySeconds = BackoffSeconds(msg.TryCount + 1);
                    await outbox.MarkFailedAsync(msg.MessageId, DateTimeOffset.UtcNow, ex.ToString(), nextRetrySeconds, cancellationToken);
                }
            }
        }

        private async Task CleanupInboxAsync(IInboxStore inbox, OutboxSettings opt, CancellationToken cancellationToken)
        {
            if (opt.InboxRetentionDays <= 0)
                return;

            var olderThan = DateTimeOffset.UtcNow.AddDays(-opt.InboxRetentionDays);
            var deleted = await inbox.CleanupAsync(olderThan, cancellationToken);
            if (deleted > 0)
                _logger.LogInformation("Inbox cleanup removed {Count} records", deleted);
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
