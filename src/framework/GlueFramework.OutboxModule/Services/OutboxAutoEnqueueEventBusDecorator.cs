using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GlueFramework.OutboxModule.Options;
using GlueFramework.OutboxModule.Settings;

namespace GlueFramework.OutboxModule.Services
{
    public sealed class OutboxAutoEnqueueEventBusDecorator : IEventBus
    {
        private readonly InProcEventBus _inner;
        private readonly IOutboxStore _outbox;
        private readonly ISiteService _siteService;
        private readonly IOptions<OutboxOptions> _configOptions;
        private readonly ILogger<OutboxAutoEnqueueEventBusDecorator> _logger;

        public OutboxAutoEnqueueEventBusDecorator(
            InProcEventBus inner,
            IOutboxStore outbox,
            ISiteService siteService,
            IOptions<OutboxOptions> configOptions,
            ILogger<OutboxAutoEnqueueEventBusDecorator> logger)
        {
            _inner = inner;
            _outbox = outbox;
            _siteService = siteService;
            _configOptions = configOptions;
            _logger = logger;
        }

        public async Task PublishAfterCommitAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        {
            if (evt is IIntegrationEvent)
            {
                try
                {
                    var opt = await LoadMergedSettingsAsync();
                    if (opt.Enabled && opt.AutoEnqueueIntegrationEvents)
                    {
                        var type = evt!.GetType().AssemblyQualifiedName ?? evt.GetType().FullName ?? typeof(TEvent).FullName ?? "Unknown";
                        var payload = JsonSerializer.Serialize(evt, evt.GetType(), OutboxJson.Options);
                        await _outbox.EnqueueAsync(type, payload, DateTimeOffset.UtcNow, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Auto-enqueue should not break the main transaction path.
                    _logger.LogError(ex, "Failed to auto-enqueue integration event to outbox.");
                }
            }

            await _inner.PublishAfterCommitAsync(evt, cancellationToken);
        }

        public Task PublishNowBestEffortAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
            => _inner.PublishNowBestEffortAsync(evt, cancellationToken);

        public Task PublishNowRequiredAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
            => _inner.PublishNowRequiredAsync(evt, cancellationToken);

        private async Task<OutboxSettings> LoadMergedSettingsAsync()
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

            var site = await _siteService.LoadSiteSettingsAsync();
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
    }
}
