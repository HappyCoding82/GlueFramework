using GlueFramework.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using GlueFramework.CustomSysSettingsModule.Abstractions;
using GlueFramework.CustomSysSettingsModule.DALInterfaces;
using GlueFramework.CustomSysSettingsModule.DataLayers;
using GlueFramework.CustomSysSettingsModule.Services;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using GlueFramework.OrchardCoreModule;
using OrchardCore.Security.Permissions;
using GlueFramework.CustomSysSettingsModule.Abstrations;
using GlueFramework.CustomSysSettingsModule.Migrations;

namespace GlueFramework.CustomSysSettingsModule
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
            services.AddScoped<IPermissionProvider, CustomSiteSettingsPermissionProvider>();
            services.AddScoped<IKeyVaultService, KeyVaultService>();
            services.AddScoped<IDataMigration, CustomSiteSettingsMigrations>();
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
