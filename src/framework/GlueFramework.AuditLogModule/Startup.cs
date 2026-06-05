using Castle.DynamicProxy;
using GlueFramework.AuditLog.Abstractions;
using GlueFramework.AuditLogModule.Correlation;
using GlueFramework.AuditLogModule.Interceptors;
using GlueFramework.AuditLogModule.Migrations;
using GlueFramework.AuditLogModule.Options;
using GlueFramework.AuditLogModule.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;
using System;
using System.Linq;
using System.Reflection;

namespace GlueFramework.AuditLogModule
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
            services.AddScoped<OrchardCore.Navigation.INavigationProvider, AdminMenu>();

            services.AddOptions<AuditLogOptions>().BindConfiguration("Diagnosis:AuditLog");

            services.TryAddSingleton<ProxyGenerator>();
            services.TryAddSingleton<CorrelationContext>();
            services.TryAddSingleton<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
            services.TryAddTransient<AuditInterceptor>();
            services.TryAddTransient<AuditStepInterceptor>();

            services.TryAddTransient<LoggerAuditWriter>();
            services.TryAddTransient<DbAuditWriter>();
            services.TryAddTransient<IAuditWriter>(sp =>
            {
                var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuditLogOptions>>().Value;
                return opt.WriterMode switch
                {
                    AuditWriterMode.Logger => sp.GetRequiredService<LoggerAuditWriter>(),
                    AuditWriterMode.Database => sp.GetRequiredService<DbAuditWriter>(),
                    _ => sp.GetRequiredService<DbAuditWriter>()
                };
            });

            services.AddScoped<IDataMigration, AuditLogMigrations>();
            // NOTE:
            // We intentionally do NOT try to proxy every service here. Business modules should register audited
            // services using an extension method (similar to AddTransactionalAuto) so that the service only pays
            // proxy overhead when [Audit] is present.
        }

        public static bool HasAnyAuditMethod(Type serviceType, Type implementationType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            bool ImplHasAttr() => implementationType
                .GetMethods(flags)
                .Any(m => m.GetCustomAttributes(typeof(AuditAttribute), true).Any());

            bool InterfaceHasAttr() => serviceType
                .GetMethods()
                .Any(m => m.GetCustomAttributes(typeof(AuditAttribute), true).Any());

            return ImplHasAttr() || InterfaceHasAttr();
        }
    }
}
