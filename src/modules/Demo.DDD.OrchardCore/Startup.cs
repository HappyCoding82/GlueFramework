using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using GlueFramework.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Data.Migration;
using Demo.DDD.OrchardCore.Application;
using Demo.DDD.OrchardCore.Application.EventHandlers;
using Demo.DDD.OrchardCore.Application.IntegrationEvents;
using Demo.DDD.OrchardCore.Domain.Events;
using Demo.DDD.OrchardCore.Infrastructure;
using Demo.DDD.OrchardCore.Migrations;
using GlueFramework.OrchardCoreModule;

namespace Demo.DDD.OrchardCore
{
    public sealed class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<SlowSqlOptions>().BindConfiguration("Diagnosis:SlowSql");
            services.AddTransient<IDbConnectionAccessor, AdapterDbAccessor>();
            services.AddTransient<IDataTablePrefixProvider, TenantTablePrefixProvider>();

            services.AddSingleton<IListingRepository, InMemoryListingRepository>();
            services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
            services.AddSingleton<IStripePayments, StripePaymentsStub>();

            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IDataMigration, DemoOutboxMigrations>();

            services.AddScoped<IEventHandler<OrderPlaced>, CreatePaymentIntentOnOrderPlaced>();
            services.AddScoped<IEventHandler<ItemVerifiedOk>, PayoutSellerOnItemVerifiedOk>();

            services.AddProxiesScopedAuto<IDemoOutboxTestAppService, DemoOutboxTestAppService>();
            services.AddScoped<IEventHandler<DemoOutboxTestRequested>, DemoOutboxTestRequestedHandler>();

            services.AddProxiesScopedAuto<IMarketplaceAppService, MarketplaceAppService>();
            services.AddScoped<IDataMigration, Migrations.DemoOutboxMigrations>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Demo.DDD.OrchardCore",
                areaName: "Demo.DDD.OrchardCore",
                pattern: "api/cards-demo/{controller=Marketplace}/{action=Index}");
        }
    }
}
