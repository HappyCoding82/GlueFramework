using GlueFramework.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using CustomSiteSettingsModule.Abstractions;
using CustomSiteSettingsModule.DALInterfaces;
using CustomSiteSettingsModule.DataLayers;
using CustomSiteSettingsModule.Services;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using GlueFramework.OrchardCoreModule;
using OrchardCore.Security.Permissions;
using CustomSiteSettingsModule.Abstrations;
using GlueFramework.CustomSysSettingsModule.Migrations;

namespace CustomSiteSettingsModule
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISiteSettingsDAL, SiteSettingsDAL>();
            services.AddTransient<IModuleServiceContext, ServiceContext>();
            services.AddTransient<ISysSettingsService, SysSettingsService>();
            services.AddTransient<IDataTablePrefixProvider, TenantTablePrefixProvider>();
            services.AddScoped<INavigationProvider, ModuleSettingNavProvider>();
            services.AddScoped<IDataMigration, CustomSiteSettingsMigrations>();
            services.AddScoped<IPermissionProvider, CustomSiteSettingsPermissionProvider>();
            services.AddScoped<IKeyVaultService, KeyVaultService>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Home",
                areaName: Constants.ModuleAreaPrefix,
                pattern: "Home/Index",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}
