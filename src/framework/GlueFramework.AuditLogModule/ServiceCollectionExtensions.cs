using Castle.DynamicProxy;
using GlueFramework.AuditLog.Abstractions;
using GlueFramework.AuditLogModule.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace GlueFramework.AuditLogModule
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuditedScopedAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            if (!Startup.HasAnyAuditMethod(typeof(TService), typeof(TImplementation)))
            {
                services.AddScoped<TService, TImplementation>();
                return services;
            }

            services.TryAddSingleton<ProxyGenerator>();
            services.TryAddTransient<AuditInterceptor>();

            services.AddScoped<TImplementation>();
            services.AddScoped(typeof(TService), sp => sp.GetRequiredService<ProxyGenerator>()
                .CreateInterfaceProxyWithTarget<TService>(
                    (TService)sp.GetRequiredService(typeof(TImplementation)),
                    sp.GetRequiredService<AuditInterceptor>()));

            return services;
        }
    }
}
