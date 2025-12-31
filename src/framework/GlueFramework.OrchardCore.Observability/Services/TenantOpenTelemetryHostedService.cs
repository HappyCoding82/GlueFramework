using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using GlueFramework.OrchardCore.Observability.Options;
using GlueFramework.OrchardCore.Observability.Settings;
using OrchardCore.Entities;
using OrchardCore.Environment.Shell;
using OrchardCore.Settings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.OrchardCore.Observability.Services
{
    public sealed class TenantOpenTelemetryHostedService : IHostedService
    {
        private readonly ISiteService _siteService;
        private readonly IOptions<ObservabilityOptions> _configOptions;
        private readonly IHostEnvironment _env;
        private readonly ShellSettings _shellSettings;
        private readonly ObservabilityRuntimeState _runtimeState;
        private readonly ILogger<TenantOpenTelemetryHostedService> _logger;

        private TracerProvider? _tracerProvider;
        private MeterProvider? _meterProvider;

        public TenantOpenTelemetryHostedService(
            ISiteService siteService,
            IOptions<ObservabilityOptions> configOptions,
            IHostEnvironment env,
            ShellSettings shellSettings,
            ObservabilityRuntimeState runtimeState,
            ILogger<TenantOpenTelemetryHostedService> logger)
        {
            _siteService = siteService;
            _configOptions = configOptions;
            _env = env;
            _shellSettings = shellSettings;
            _runtimeState = runtimeState;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Merge defaults (appsettings) + tenant settings (DB overrides).
                var opt = new ObservabilityOptions
                {
                    Enabled = _configOptions.Value.Enabled,
                    OtlpEndpoint = _configOptions.Value.OtlpEndpoint,
                    TraceSampleRate = _configOptions.Value.TraceSampleRate,
                    EnableAspNetCoreInstrumentation = _configOptions.Value.EnableAspNetCoreInstrumentation,
                    EnableHttpClientInstrumentation = _configOptions.Value.EnableHttpClientInstrumentation,
                    EnableRuntimeMetrics = _configOptions.Value.EnableRuntimeMetrics,
                    DashboardUrl = _configOptions.Value.DashboardUrl,
                    TracesUrl = _configOptions.Value.TracesUrl,
                    MetricsUrl = _configOptions.Value.MetricsUrl,
                };

                var site = await _siteService.LoadSiteSettingsAsync();
                var settings = site.As<ObservabilitySettings>();
                if (settings != null)
                {
                    opt.Enabled = settings.Enabled;
                    opt.OtlpEndpoint = settings.OtlpEndpoint;
                    opt.TraceSampleRate = settings.TraceSampleRate;
                    opt.EnableAspNetCoreInstrumentation = settings.EnableAspNetCoreInstrumentation;
                    opt.EnableHttpClientInstrumentation = settings.EnableHttpClientInstrumentation;
                    opt.EnableRuntimeMetrics = settings.EnableRuntimeMetrics;
                    opt.DashboardUrl = settings.DashboardUrl;
                    opt.TracesUrl = settings.TracesUrl;
                    opt.MetricsUrl = settings.MetricsUrl;
                }

                _runtimeState.Started = false;
                _runtimeState.AppliedOtlpEndpoint = opt.OtlpEndpoint;
                _runtimeState.AppliedTraceSampleRate = opt.TraceSampleRate;

                if (!opt.Enabled)
                {
                    _logger.LogInformation("OpenTelemetry disabled for tenant {Tenant}.", _shellSettings.Name ?? "Default");
                    return;
                }

                if (opt.TraceSampleRate < 0)
                    opt.TraceSampleRate = 0;
                if (opt.TraceSampleRate > 1)
                    opt.TraceSampleRate = 1;

                var tenantName = _shellSettings.Name ?? "Default";

                _logger.LogInformation(
                    "Starting OpenTelemetry for tenant {Tenant}. Endpoint={Endpoint}, SampleRate={SampleRate}",
                    tenantName,
                    opt.OtlpEndpoint,
                    opt.TraceSampleRate);

                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "Demo.Orchard")
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", _env.EnvironmentName),
                        new KeyValuePair<string, object>("tenant.name", tenantName)
                    });

                // Traces
                var tracerBuilder = Sdk.CreateTracerProviderBuilder()
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("Framework.Core")
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(opt.TraceSampleRate)));

                if (opt.EnableAspNetCoreInstrumentation)
                    tracerBuilder.AddAspNetCoreInstrumentation();

                if (opt.EnableHttpClientInstrumentation)
                    tracerBuilder.AddHttpClientInstrumentation();

                tracerBuilder.AddOtlpExporter(o =>
                {
                    if (!string.IsNullOrWhiteSpace(opt.OtlpEndpoint))
                        o.Endpoint = new Uri(opt.OtlpEndpoint);

                    o.Protocol = OtlpExportProtocol.Grpc;
                });

                _tracerProvider = tracerBuilder.Build();

                // Metrics
                var meterBuilder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(resourceBuilder);

                if (opt.EnableRuntimeMetrics)
                    meterBuilder.AddRuntimeInstrumentation();

                meterBuilder.AddOtlpExporter(o =>
                {
                    if (!string.IsNullOrWhiteSpace(opt.OtlpEndpoint))
                        o.Endpoint = new Uri(opt.OtlpEndpoint);

                    o.Protocol = OtlpExportProtocol.Grpc;
                });

                _meterProvider = meterBuilder.Build();

                _runtimeState.Started = true;
                _runtimeState.AppliedOtlpEndpoint = opt.OtlpEndpoint;
                _runtimeState.AppliedTraceSampleRate = opt.TraceSampleRate;
                _runtimeState.StartedUtc = DateTimeOffset.UtcNow;
                _runtimeState.LastError = null;
                _runtimeState.LastErrorUtc = null;
            }
            catch (Exception ex)
            {
                _runtimeState.Started = false;
                _runtimeState.LastError = ex.ToString();
                _runtimeState.LastErrorUtc = DateTimeOffset.UtcNow;
                _logger.LogError(ex, "Failed to start OpenTelemetry for tenant {Tenant}.", _shellSettings.Name ?? "Default");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
            return Task.CompletedTask;
        }
    }
}
