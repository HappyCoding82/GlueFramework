namespace GlueFramework.OrchardCore.Observability.Options
{
    public sealed class ObservabilityOptions
    {
        public bool Enabled { get; set; } = false;

        public string? OtlpEndpoint { get; set; }

        public double TraceSampleRate { get; set; } = 0.01;

        public bool EnableAspNetCoreInstrumentation { get; set; } = true;

        public bool EnableHttpClientInstrumentation { get; set; } = true;

        public bool EnableRuntimeMetrics { get; set; } = true;

        public string? DashboardUrl { get; set; }

        public string? TracesUrl { get; set; }

        public string? MetricsUrl { get; set; }
    }
}
