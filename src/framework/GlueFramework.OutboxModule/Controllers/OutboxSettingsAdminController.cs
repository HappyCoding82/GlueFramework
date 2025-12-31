using GlueFramework.OutboxModule.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;
using OrchardCore.Entities;
using OrchardCore.Settings;

namespace GlueFramework.OutboxModule.Controllers
{
    [Admin]
    public sealed class OutboxSettingsAdminController : Controller
    {
        private readonly ISiteService _siteService;
        private readonly IAuthorizationService _authorizationService;

        public OutboxSettingsAdminController(ISiteService siteService, IAuthorizationService authorizationService)
        {
            _siteService = siteService;
            _authorizationService = authorizationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var site = await _siteService.LoadSiteSettingsAsync();
            var settings = site.As<OutboxSettings>() ?? new OutboxSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(OutboxSettings model)
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
                s.InboxRetentionDays = model.InboxRetentionDays;
                s.EnableInboxCleanup = model.EnableInboxCleanup;
            });
            await _siteService.UpdateSiteSettingsAsync(site);

            return RedirectToAction(nameof(Index));
        }
    }
}
