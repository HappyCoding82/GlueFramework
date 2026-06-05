namespace GlueFramework.OrchardCore.Observability.ViewModels
{
    public sealed class ObservabilitySettingsViewModel
    {
        public bool Enabled { get; set; }

        public string? OtlpEndpoint { get; set; }

        public double TraceSampleRate { get; set; }

        public bool EnableAspNetCoreInstrumentation { get; set; }

        public bool EnableHttpClientInstrumentation { get; set; }

        public bool EnableRuntimeMetrics { get; set; }

        public string? DashboardUrl { get; set; }

        public string? TracesUrl { get; set; }

        public string? MetricsUrl { get; set; }
    }
}
