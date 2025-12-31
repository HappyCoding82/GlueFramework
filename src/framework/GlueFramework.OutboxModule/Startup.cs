using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using OrchardCore.Modules;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.Core.Services;
using GlueFramework.OutboxModule.Infrastructure;
using GlueFramework.OutboxModule.Options;
using GlueFramework.OutboxModule.Services;
using OrchardCore.Data.Migration;
using GlueFramework.OutboxModule.Migrations;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.BackgroundTasks;

namespace GlueFramework.OutboxModule
{
    public sealed class Startup : StartupBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation("Framework.OutboxModule Startup.ConfigureServices executing");

            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            services.AddOptions<OutboxOptions>().BindConfiguration("Outbox");

            // Ensure base in-proc event bus exists so the decorator can delegate to it.
            services.TryAddSingleton<InProcEventBus>();

            // Must be scoped because the tenant table prefix provider depends on scoped YesSql.ISession.
            services.TryAddScoped<IOutboxStore, SqlOutboxStore>();
            services.TryAddScoped<IInboxStore, SqlInboxStore>();
            services.TryAddScoped<IOutboxEnqueuer, OutboxEnqueuer>();

            services.TryAddScoped<OutboxAutoEnqueueEventBusDecorator>();
            // Must override any existing IEventBus registration (e.g. AddInProcEventBus).
            services.Replace(ServiceDescriptor.Scoped<GlueFramework.Core.Abstractions.IEventBus>(sp =>
                sp.GetRequiredService<OutboxAutoEnqueueEventBusDecorator>()));

            services.AddScoped<IBackgroundTask, OutboxDispatcherBackgroundTask>();
            services.AddScoped<IDataMigration, OutboxMigrations>();
        }

        //public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        //{
        //    app.UseMiddleware<TenantOutboxDispatcherMiddleware>();
        //}
    }
}
