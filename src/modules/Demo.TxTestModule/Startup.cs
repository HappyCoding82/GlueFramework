using Demo.TxTestModule.Abstractions;
using Demo.TxTestModule.Application;
using Demo.TxTestModule.Migrations;
using Demo.TxTestModule.Infrastructure;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.DataLayer;
using GlueFramework.Core.Diagnostics;
using GlueFramework.Core.Extensions;
using GlueFramework.OrchardCoreModule;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;

namespace Demo.TxTestModule;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<SlowSqlOptions>().BindConfiguration("Diagnosis:SlowSql");
        services.AddTransient<IDbConnectionAccessor, AdapterDbAccessor>();
        services.AddTransient<IDataTablePrefixProvider, TenantTablePrefixProvider>();
        services.AddScoped<IDALFactory, DALFactory>();

        services.AddScoped<IDataMigration, TxTestMigrations>();

        services.AddTransient<ITxTestRepository, TxTestRepository>();

        services.AddProxiesScopedAuto<ITxTestServiceB, TxTestServiceB>();
        services.AddProxiesScopedAuto<ITxTestService, TxTestService>();

        services.AddControllers();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Demo.TxTestModule",
            areaName: "Demo.TxTestModule",
            pattern: "api/tx-test/{action}",
            defaults: new { controller = "TxTest" });
    }
}
