using GlueFramework.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GlueFramework.OrchardCoreModule
{
    public class TenantTablePrefixProvider : ITenantTableSettingsProvider
    {
        private readonly IServiceProvider _serviceProvider;
        //YesSql.ISession _session;
        public TenantTablePrefixProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            //_session = session;
        }

        public string? Schema
        {
            get
            {
                var session = _serviceProvider.GetService<YesSql.ISession>();
                if (session == null)
                    return null;

                // YesSql Store Configuration may expose Schema depending on provider.
                var cfg = session.Store.Configuration;
                var pi = cfg.GetType().GetProperty("Schema");
                var v = pi?.GetValue(cfg) as string;
                return string.IsNullOrWhiteSpace(v) ? null : v;
            }
        }

        public string Prefix
        {
            get
            {
                var session = _serviceProvider.GetService<YesSql.ISession>();
                return session.Store.Configuration.TablePrefix;
            }
        }
        //=> _session.Store.Configuration.TablePrefix;
    }
}
