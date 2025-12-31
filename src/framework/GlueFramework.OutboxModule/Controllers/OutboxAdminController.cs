using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;
using GlueFramework.OutboxModule.Options;
using GlueFramework.OutboxModule.Settings;
using GlueFramework.Core.Abstractions.Outbox;

namespace GlueFramework.OutboxModule.Controllers
{
    [Admin]
    public sealed class OutboxAdminController : Controller
    {
        private readonly IOutboxStore _outbox;
        private readonly IInboxStore _inbox;
        private readonly ISiteService _siteService;
        private readonly IOptions<OutboxOptions> _configOptions;

        public OutboxAdminController(IOutboxStore outbox, IInboxStore inbox, ISiteService siteService, IOptions<OutboxOptions> configOptions)
        {
            _outbox = outbox;
            _inbox = inbox;
            _siteService = siteService;
            _configOptions = configOptions;
        }

        [HttpGet]
        public async Task<IActionResult> Outbox([FromQuery] int take = 50, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
        {
            var items = await _outbox.QueryAsync(Math.Clamp(take, 1, 500), status, cancellationToken);
            return Ok(items);
        }

        [HttpGet]
        public async Task<IActionResult> InboxCount(CancellationToken cancellationToken = default)
        {
            var count = await _inbox.CountAsync(cancellationToken);
            return Ok(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Settings(CancellationToken cancellationToken = default)
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            var settings = site.As<OutboxSettings>() ?? new OutboxSettings();

            return Ok(new
            {
                site = settings,
                config = _configOptions.Value
            });
        }

        [HttpPost]
        public async Task<IActionResult> Settings([FromBody] OutboxSettings model, CancellationToken cancellationToken = default)
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            site.Alter<OutboxSettings>(s =>
            {
                s.Enabled = model.Enabled;
                s.AutoEnqueueIntegrationEvents = model.AutoEnqueueIntegrationEvents;
                s.DispatchIntervalSeconds = model.DispatchIntervalSeconds;
                s.BatchSize = model.BatchSize;
                s.InboxRetentionDays = model.InboxRetentionDays;
                s.EnableInboxCleanup = model.EnableInboxCleanup;
            });
            await _siteService.UpdateSiteSettingsAsync(site);
            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> CleanupInbox([FromQuery] int? retentionDays = null, CancellationToken cancellationToken = default)
        {
            var days = retentionDays ?? _configOptions.Value.InboxRetentionDays;
            if (days <= 0)
                return BadRequest(new { error = "retentionDays must be > 0" });

            var olderThan = DateTimeOffset.UtcNow.AddDays(-days);
            var deleted = await _inbox.CleanupAsync(olderThan, cancellationToken);
            return Ok(new { deleted, olderThanUtc = olderThan });
        }
    }
}
