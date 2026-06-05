using GlueFramework.Core.DataAccessors;
using GlueFramework.Core.ContextCaches;
using GlueFramework.Core.ORM;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;
using GlueFramework.AuditLog.Abstractions;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace GlueFramework.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInProcEventBus(this IServiceCollection services)
        {
            services.TryAddSingleton<InProcEventBus>();
            services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<InProcEventBus>());
            return services;
        }

        public static void UseSqlServerDataAccessor(this IServiceCollection services)
        {
            services.AddTransient<Abstractions.IDbConnectionAccessor, SqlServerDataAccessor>();
        }

        public static void UseSqlServerDataAccessor(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<Abstractions.IDbConnectionAccessor>(SqlServerDataAccessor.CreateDataAccessor(connectionString));
        }

        public static void UseDefaultDataTablePrefixProvider(this IServiceCollection services)
        {
            services.AddTransient<Abstractions.IDataTablePrefixProvider, DefaultDataTablePrefixProvider>();
        }

        public static IServiceCollection AddTransactionalScoped<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransactional<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddTransactionalScopedAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransactionalAuto<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddTransactionalTransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransactional<TService, TImplementation>(ServiceLifetime.Transient);
        }

        public static IServiceCollection AddTransactionalTransientAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransactionalAuto<TService, TImplementation>(ServiceLifetime.Transient);
        }

        public static IServiceCollection AddTransactionalSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransactional<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddTransactionalSingletonAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransactionalAuto<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddContextCachedScoped<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddContextCached<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddContextCachedTransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddContextCached<TService, TImplementation>(ServiceLifetime.Transient);
        }

        public static IServiceCollection AddContextCachedSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddContextCached<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddContextCached<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            

            services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));

            services.Add(new ServiceDescriptor(
                typeof(TService),
                sp => sp.GetRequiredService<ProxyGenerator>()
                    .CreateInterfaceProxyWithTarget<TService>(
                        (TService)sp.GetRequiredService(typeof(TImplementation)),
                        sp.GetRequiredService<MemoryCacheInterceptor>()),
                lifetime));

            return services;
        }

        public static IServiceCollection AddTransactional<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            services.TryAddSingleton<ProxyGenerator>();
            services.TryAddTransient<TransactionInterceptor>();

            services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));

            services.Add(new ServiceDescriptor(
                typeof(TService),
                sp => sp.GetRequiredService<ProxyGenerator>()
                    .CreateInterfaceProxyWithTarget<TService>(
                        (TService)sp.GetRequiredService(typeof(TImplementation)),
                        sp.GetRequiredService<TransactionInterceptor>()),
                lifetime));

            return services;
        }

        public static IServiceCollection AddTransactionalAuto<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            if (!HasAnyTransactionalMethod(typeof(TService), typeof(TImplementation)))
            {
                services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
                return services;
            }

            return services.AddTransactional<TService, TImplementation>(lifetime);
        }

        public static IServiceCollection AddProxiesScopedAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddProxiesAuto<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddProxiesTransientAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddProxiesAuto<TService, TImplementation>(ServiceLifetime.Transient);
        }

        public static IServiceCollection AddProxiesSingletonAuto<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddProxiesAuto<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddProxiesAuto<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            var serviceType = typeof(TService);
            var implType = typeof(TImplementation);

            var needsTx = HasAnyTransactionalMethod(serviceType, implType);
            var needsCache = HasAnyContextCacheMethod(serviceType, implType);
            var needsAudit = HasAnyAuditMethod(serviceType, implType);
            var needsAuditStep = HasAnyAuditStepMethod(serviceType, implType);

            if (!needsTx && !needsCache && !needsAudit && !needsAuditStep)
            {
                services.Add(new ServiceDescriptor(serviceType, implType, lifetime));
                return services;
            }

            services.TryAddSingleton<ProxyGenerator>();

            if (needsTx)
                services.TryAddTransient<TransactionInterceptor>();

            if (needsCache)
                services.TryAddTransient<MemoryCacheInterceptor>();

            services.Add(new ServiceDescriptor(implType, implType, lifetime));

            services.Add(new ServiceDescriptor(
                serviceType,
                sp =>
                {
                    var proxyGen = sp.GetRequiredService<ProxyGenerator>();
                    var target = (TService)sp.GetRequiredService(implType);

                    // Order matters:
                    // - Audit/AuditStep outermost: record even on cache-hit
                    // - Cache before Transaction: cache-hit should bypass transaction
                    // - Transaction innermost
                    var interceptors = BuildInterceptors(sp, needsAudit, needsAuditStep, needsCache, needsTx);
                    return proxyGen.CreateInterfaceProxyWithTarget<TService>(target, interceptors);
                },
                lifetime));

            return services;
        }

        private static IInterceptor[] BuildInterceptors(IServiceProvider sp, bool needsAudit, bool needsAuditStep, bool needsCache, bool needsTx)
        {
            // Upper bound: Audit + AuditStep + Cache + Tx
            var list = new System.Collections.Generic.List<IInterceptor>(capacity: 4);

            if (needsAudit)
            {
                var audit = TryResolveAuditInterceptor(sp);
                if (audit != null)
                    list.Add(audit);
            }

            if (needsAuditStep)
            {
                var auditStep = TryResolveAuditStepInterceptor(sp);
                if (auditStep != null)
                    list.Add(auditStep);
            }

            if (needsCache)
                list.Add(sp.GetRequiredService<MemoryCacheInterceptor>());

            if (needsTx)
                list.Add(sp.GetRequiredService<TransactionInterceptor>());

            return list.ToArray();
        }

        private static IInterceptor? TryResolveAuditInterceptor(IServiceProvider sp)
        {
            // Audit interceptor lives in Framework.AuditLogModule. Core must not reference it.
            const string auditInterceptorTypeName = "Framework.AuditLogModule.Interceptors.AuditInterceptor";

            Type? t = null;
            try
            {
                t = Type.GetType(auditInterceptorTypeName, throwOnError: false);
                if (t == null)
                {
                    t = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(auditInterceptorTypeName, throwOnError: false, ignoreCase: false))
                        .FirstOrDefault(x => x != null);
                }
            }
            catch
            {
                t = null;
            }

            if (t == null)
                return null;

            var svc = sp.GetService(t);
            return svc as IInterceptor;
        }

        private static IInterceptor? TryResolveAuditStepInterceptor(IServiceProvider sp)
        {
            // AuditStep interceptor lives in Framework.AuditLogModule. Core must not reference it.
            const string auditStepInterceptorTypeName = "Framework.AuditLogModule.Interceptors.AuditStepInterceptor";

            Type? t = null;
            try
            {
                t = Type.GetType(auditStepInterceptorTypeName, throwOnError: false);
                if (t == null)
                {
                    t = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(auditStepInterceptorTypeName, throwOnError: false, ignoreCase: false))
                        .FirstOrDefault(x => x != null);
                }
            }
            catch
            {
                t = null;
            }

            if (t == null)
                return null;

            var svc = sp.GetService(t);
            return svc as IInterceptor;
        }

        private static bool HasAnyContextCacheMethod(Type serviceType, Type implementationType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            bool ImplHasAttr() => implementationType
                .GetMethods(flags)
                .Any(m => m.GetCustomAttributes(typeof(ContextCacheAttribute), true).Any());

            bool InterfaceHasAttr() => serviceType
                .GetMethods()
                .Any(m => m.GetCustomAttributes(typeof(ContextCacheAttribute), true).Any());

            return ImplHasAttr() || InterfaceHasAttr();
        }

        private static bool HasAnyAuditStepMethod(Type serviceType, Type implementationType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            bool ImplHasAttr() => implementationType
                .GetMethods(flags)
                .Any(m => m.GetCustomAttributes(typeof(AuditStepAttribute), true).Any());

            bool InterfaceHasAttr() => serviceType
                .GetMethods()
                .Any(m => m.GetCustomAttributes(typeof(AuditStepAttribute), true).Any());

            return ImplHasAttr() || InterfaceHasAttr();
        }

        private static bool HasAnyAuditMethod(Type serviceType, Type implementationType)
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

        private static bool HasAnyTransactionalMethod(Type serviceType, Type implementationType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            bool ImplHasAttr() => implementationType
                .GetMethods(flags)
                .Any(m => m.GetCustomAttributes(typeof(TransactionalAttribute), true).Any());

            bool InterfaceHasAttr() => serviceType
                .GetMethods()
                .Any(m => m.GetCustomAttributes(typeof(TransactionalAttribute), true).Any());

            return ImplHasAttr() || InterfaceHasAttr();
        }
    }
}
