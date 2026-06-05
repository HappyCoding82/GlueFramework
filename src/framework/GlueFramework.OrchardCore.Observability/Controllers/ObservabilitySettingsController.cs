using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using GlueFramework.OrchardCore.Observability.Options;
using GlueFramework.OrchardCore.Observability.Settings;
using GlueFramework.OrchardCore.Observability.ViewModels;
using OrchardCore.Entities;
using OrchardCore.Environment.Shell;
using OrchardCore.Settings;
using System.Threading.Tasks;

namespace GlueFramework.OrchardCore.Observability.Controllers
{
    [Admin]
    public sealed class ObservabilitySettingsController : Controller
    {
        private readonly ISiteService _siteService;
        private readonly IOptions<ObservabilityOptions> _configOptions;
        private readonly ShellSettings _shellSettings;

        public ObservabilitySettingsController(ISiteService siteService, IOptions<ObservabilityOptions> configOptions, ShellSettings shellSettings)
        {
            _siteService = siteService;
            _configOptions = configOptions;
            _shellSettings = shellSettings;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            var settings = site.As<ObservabilitySettings>() ?? new ObservabilitySettings();

            var vm = new ObservabilitySettingsViewModel
            {
                Enabled = settings.Enabled,
                OtlpEndpoint = settings.OtlpEndpoint,
                TraceSampleRate = settings.TraceSampleRate,
                EnableAspNetCoreInstrumentation = settings.EnableAspNetCoreInstrumentation,
                EnableHttpClientInstrumentation = settings.EnableHttpClientInstrumentation,
                EnableRuntimeMetrics = settings.EnableRuntimeMetrics,
                DashboardUrl = settings.DashboardUrl,
                TracesUrl = settings.TracesUrl,
                MetricsUrl = settings.MetricsUrl
            };

            ViewBag.ConfigEnabled = _configOptions.Value.Enabled;
            ViewBag.ConfigOtlpEndpoint = _configOptions.Value.OtlpEndpoint;
            ViewBag.ConfigTraceSampleRate = _configOptions.Value.TraceSampleRate;
            ViewBag.TenantName = _shellSettings.Name ?? "Default";

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ObservabilitySettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var site = await _siteService.LoadSiteSettingsAsync();
            site.Alter<ObservabilitySettings>(s =>
            {
                s.Enabled = model.Enabled;
                s.OtlpEndpoint = model.OtlpEndpoint;
                s.TraceSampleRate = model.TraceSampleRate;
                s.EnableAspNetCoreInstrumentation = model.EnableAspNetCoreInstrumentation;
                s.EnableHttpClientInstrumentation = model.EnableHttpClientInstrumentation;
                s.EnableRuntimeMetrics = model.EnableRuntimeMetrics;
                s.DashboardUrl = model.DashboardUrl;
                s.TracesUrl = model.TracesUrl;
                s.MetricsUrl = model.MetricsUrl;
            });

            await _siteService.UpdateSiteSettingsAsync(site);

            var readbackSite = await _siteService.GetSiteSettingsAsync();
            var readback = readbackSite.As<ObservabilitySettings>() ?? new ObservabilitySettings();
            TempData["SavedReadback"] = $"Readback: Enabled={readback.Enabled}, OtlpEndpoint={readback.OtlpEndpoint}, TraceSampleRate={readback.TraceSampleRate}";

            TempData["StatusMessage"] = "Observability settings saved. Restart tenant/app for tracing/metrics changes to fully apply.";
            return RedirectToAction(nameof(Index));
        }
    }
}
