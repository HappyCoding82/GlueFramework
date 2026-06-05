using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using GlueFramework.OrchardCore.Observability.Options;
using GlueFramework.OrchardCore.Observability.Settings;
using GlueFramework.OrchardCore.Observability.Services;
using GlueFramework.OrchardCore.Observability.ViewModels;
using OrchardCore.Entities;
using OrchardCore.Environment.Shell;
using OrchardCore.Settings;
using System.Threading.Tasks;

namespace GlueFramework.OrchardCore.Observability.Controllers
{
    [Admin]
    public sealed class ObservabilityAdminController : Controller
    {
        private readonly IOptions<ObservabilityOptions> _options;
        private readonly ISiteService _siteService;
        private readonly ShellSettings _shellSettings;
        private readonly ObservabilityRuntimeState _runtimeState;

        public ObservabilityAdminController(IOptions<ObservabilityOptions> options, ISiteService siteService, ShellSettings shellSettings, ObservabilityRuntimeState runtimeState)
        {
            _options = options;
            _siteService = siteService;
            _shellSettings = shellSettings;
            _runtimeState = runtimeState;
        }

        public async Task<IActionResult> Index()
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            var settings = site.As<ObservabilitySettings>() ?? new ObservabilitySettings();

            var vm = new ObservabilityStatusViewModel
            {
                TenantName = _shellSettings.Name ?? "Default",
                Config = _options.Value,
                Tenant = settings,
                Runtime = _runtimeState
            };

            return View(vm);
        }
    }
}
