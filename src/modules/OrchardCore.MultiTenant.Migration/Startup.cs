using Tierdrop.DBMigration.Abstractions;
using Tierdrop.DBMigration.Services;
using StartupBase = OrchardCore.Modules.StartupBase;

namespace OrchardCore.MultiTenant.Migration
{
    public sealed class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITenantDbContextFactory, TenantDbContextFactory>();
            services.AddTransient<IMigrationScriptProvider, EmbeddedMigrationScriptProvider>();
            services.AddTransient<UpdateTenantDbHandler>();
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var handler = serviceProvider.GetRequiredService<UpdateTenantDbHandler>();
            handler.RunAsync().GetAwaiter().GetResult();
        }
    }
}
