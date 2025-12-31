using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Security.Permissions;
using Demo.CustomModule.Filters;
using Demo.CustomModule.Permissions;

namespace Demo.CustomModule
{
    public class Startup : OrchardCore.Modules.StartupBase
    {
        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _env;
        
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPermissionProvider, ApiPermissions>();
            services.AddScoped<PermissionAuthorizationFilter>();

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.AddService<PermissionAuthorizationFilter>();
            });
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            // 配置路由和中间件
        }
    }
}