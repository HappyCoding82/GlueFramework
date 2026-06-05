using OrchardCore.Environment.Shell;
using OrchardCore.MultiTenant.Migration.Executor;
using OrchardCore.MultiTenant.Migration.Helpers;
using Tierdrop.DBMigration.Abstractions;

namespace OrchardCore.MultiTenant.Migration
{
    public class UpdateTenantDbHandler
    {
        private readonly IShellSettingsManager _shellSettingsManager;
        private readonly IMigrationScriptProvider _scriptProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;

        public UpdateTenantDbHandler(IShellSettingsManager shellSettingsManager,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env,
            IMigrationScriptProvider scriptProvider)
        {
            _env = env;
            _scriptProvider = scriptProvider;
            _httpContextAccessor = httpContextAccessor;
            _shellSettingsManager = shellSettingsManager;
        }

        public async Task RunAsync()
        {
            var targetTenant = _httpContextAccessor.HttpContext?.Features.Get<ShellContextFeature>()?.ShellContext.Settings.Name;
            if (targetTenant == null)
            {
                return;
            }

            var tenants = await _shellSettingsManager.LoadSettingsAsync();

            foreach (var tenant in tenants)
            {
                if (targetTenant != tenant.Name) continue;
                if (!tenant.IsInitialized()) continue;

                var provider = TenantProviderHelper.NormalizeProvider(tenant["DatabaseProvider"]);
                var connectionString = tenant["ConnectionString"];
                var prefix = (tenant["TablePrefix"] ?? string.Empty) + "_";
                var schema = tenant["Schema"] ?? string.Empty;

                if (string.IsNullOrWhiteSpace(connectionString)) continue;

                await using var conn = TenantProviderHelper.CreateConnection(provider, connectionString);
                await conn.OpenAsync();

                MigrationExecutor executor = provider switch
                {
                    "SqlServer" => new SqlServerMigrationExecutor(conn, prefix, _env, schema),
                    "PostgreSQL" => new PostgreSqlMigrationExecutor(conn, prefix, _env,schema),
                    _ => throw new NotSupportedException(provider)
                };

                await executor.EnsureHistoryTableAsync();

                //var scripts = _scriptProvider.GetScripts(provider);
                var scripts = _scriptProvider.GetScripts(provider, connectionString);
                foreach (var script in scripts)
                {
                    foreach (var batch in SqlScriptProcessor.SplitSql(script.Sql, provider))
                    {
                        var finalSql = executor.ApplyPrefix(batch, prefix);
                        await executor.ExecuteBatchAsync(finalSql);
                    }
                }
            }
        }
    }

}
