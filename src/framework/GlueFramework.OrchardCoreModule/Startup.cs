using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using GlueFramework.Core.DataLayer;
using GlueFramework.Core.Extensions;
//using GlueFramework.OrchardCoreM;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchardCore.Modules;
using GlueFramework.WebCore.Extensions;

namespace GlueFramework.OrchardCoreModule
{
    public class Startup : StartupBase
    {
        public IConfiguration Configuration { get; }

        private Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        public Startup(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;

            //string configFile = _env.IsDevelopment() ? "scribeadminmodule.dev.json" : "scribeadminmodule.json";
            //var builder = new ConfigurationBuilder()
            //.AddJsonFile(configFile, optional: false, reloadOnChange: true)
            //.AddEnvironmentVariables();
            //this.Configuration = builder.Build();
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddFrameworkSupport(Configuration, _env);

            services.Configure<SlowSqlOptions>(Configuration.GetSection("Diagnosis:SlowSql"));
            services.AddTransient<GlueFramework.Core.Abstractions.IDbConnectionAccessor, AdapterDbAccessor>();
            services.AddTransient<IDataTablePrefixProvider, TenantTablePrefixProvider>();
            services.AddSingleton<IDALFactory, DALFactory>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Home",
                areaName: "GlueFramework.OrchardCoreModule",
                pattern: "Home/Index",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}
