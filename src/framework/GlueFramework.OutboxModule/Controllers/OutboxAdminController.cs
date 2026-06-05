using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Entities;
using OrchardCore.Settings;
using GlueFramework.OutboxModule.Options;
using GlueFramework.OutboxModule.Settings;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.OutboxModule.Services;

namespace GlueFramework.OutboxModule.Controllers
{
    [Admin]
    public sealed class OutboxAdminController : Controller
    {
        private readonly OutboxService _outbox;
        private readonly IInboxStore _inbox;
        private readonly ISiteService _siteService;
        private readonly IOptions<OutboxOptions> _configOptions;
        private readonly IAuthorizationService _authorizationService;

        public OutboxAdminController(OutboxService outbox, IInboxStore inbox, ISiteService siteService, IOptions<OutboxOptions> configOptions, IAuthorizationService authorizationService)
        {
            _outbox = outbox;
            _inbox = inbox;
            _siteService = siteService;
            _configOptions = configOptions;
            _authorizationService = authorizationService;
        }

        [HttpGet]
        public async Task<IActionResult> Outbox([FromQuery] int take = 50, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var items = await _outbox.QueryAsync(Math.Clamp(take, 1, 500), status, cancellationToken);
            return Ok(items);
        }

        [HttpGet]
        public async Task<IActionResult> InboxCount(CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var count = await _inbox.CountAsync(cancellationToken);
            return Ok(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Settings(CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

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
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var site = await _siteService.LoadSiteSettingsAsync();
            site.Alter<OutboxSettings>(s =>
            {
                s.Enabled = model.Enabled;
                s.AutoEnqueueIntegrationEvents = model.AutoEnqueueIntegrationEvents;
                s.DispatchIntervalSeconds = model.DispatchIntervalSeconds;
                s.BatchSize = model.BatchSize;
                s.OutboxRetentionDays = model.OutboxRetentionDays;
                s.EnableOutboxCleanup = model.EnableOutboxCleanup;
                s.InboxRetentionDays = model.InboxRetentionDays;
                s.EnableInboxCleanup = model.EnableInboxCleanup;
            });
            await _siteService.UpdateSiteSettingsAsync(site);
            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> CleanupInbox([FromQuery] int? retentionDays = null, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var days = retentionDays ?? _configOptions.Value.InboxRetentionDays;
            if (days <= 0)
                return BadRequest(new { error = "retentionDays must be > 0" });

            var olderThan = DateTimeOffset.UtcNow.AddDays(-days);
            var deleted = await _inbox.CleanupAsync(olderThan, cancellationToken);
            return Ok(new { deleted, olderThanUtc = olderThan });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOutboxStatus([FromQuery] Guid messageId, [FromQuery] string status, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var ok = await _outbox.UpdateStatusAsync(messageId, status, cancellationToken);
            return Ok(new { ok });
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveOutbox([FromQuery] Guid messageId, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var ok = await _outbox.ArchiveAsync(messageId, cancellationToken);
            return Ok(new { ok });
        }
    }
}
