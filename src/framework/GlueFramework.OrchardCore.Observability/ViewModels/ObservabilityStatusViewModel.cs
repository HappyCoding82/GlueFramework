using GlueFramework.OrchardCore.Observability.Options;
using GlueFramework.OrchardCore.Observability.Settings;
using GlueFramework.OrchardCore.Observability.Services;

namespace GlueFramework.OrchardCore.Observability.ViewModels
{
    public sealed class ObservabilityStatusViewModel
    {
        public string TenantName { get; set; } = "Default";

        public ObservabilityOptions Config { get; set; } = new ObservabilityOptions();

        public ObservabilitySettings Tenant { get; set; } = new ObservabilitySettings();

        public ObservabilityRuntimeState Runtime { get; set; } = new ObservabilityRuntimeState();
    }
}
