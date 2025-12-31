using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using GlueFramework.OrchardCore.Observability.Options;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;
using GlueFramework.OrchardCore.Observability.Services;
using OrchardCore.Navigation;

namespace GlueFramework.OrchardCore.Observability
{
    public sealed class Startup : StartupBase
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            services.AddOptions<ObservabilityOptions>().BindConfiguration("Diagnosis:OpenTelemetry");

            services.PostConfigure<ObservabilityOptions>(opt =>
            {
                // Ensure defaults and safety.
                if (opt.TraceSampleRate < 0)
                    opt.TraceSampleRate = 0;
                if (opt.TraceSampleRate > 1)
                    opt.TraceSampleRate = 1;
            });

            // Create OpenTelemetry providers after the tenant container is fully built so we can safely
            // load tenant SiteSettings and avoid BuildServiceProvider() anti-pattern.
            services.AddSingleton<ObservabilityRuntimeState>();
            services.AddSingleton<TenantOpenTelemetryManager>();
            services.AddTransient<TenantOpenTelemetryMiddleware>();
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            app.UseMiddleware<TenantOpenTelemetryMiddleware>();
        }
    }
}
