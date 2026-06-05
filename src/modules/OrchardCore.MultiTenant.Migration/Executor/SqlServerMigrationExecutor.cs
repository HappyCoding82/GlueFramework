using System.Data.Common;
using System.Text.RegularExpressions;

namespace OrchardCore.MultiTenant.Migration.Executor
{
    public class SqlServerMigrationExecutor : MigrationExecutor
    {
        private readonly string _schema;

        public SqlServerMigrationExecutor(DbConnection connection, string tablePrefix, IWebHostEnvironment env, string schema = "dbo")
            : base(connection, tablePrefix, env)
        {
            _schema = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
        }

        public override string Provider => "SqlServer";

        public override async Task EnsureHistoryTableAsync()
        {
            var tableName = $"{TablePrefix}__EFMigrationsHistory";
            string sql = $@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{_schema}' AND TABLE_NAME = '{tableName}')
                BEGIN
                    CREATE TABLE [{_schema}].[{tableName}](
                      [MigrationId] [nvarchar](150) NOT NULL,
                      [ProductVersion] [nvarchar](32) NOT NULL,
                        CONSTRAINT [PK{tableName}] PRIMARY KEY ([MigrationId])
                    )
                END";
            await ExecuteBatchAsync(sql);
        }

        public override string ApplyPrefix(string sql, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return sql;
            const int MaxConstraintLength = 128;
            // table name
            var tableRegex = new Regex(@"(CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS|CREATE\s+TABLE|ALTER\s+TABLE|DROP\s+TABLE|INSERT\s+INTO|REFERENCES|UPDATE)\s+\[?(\w+)\]?", RegexOptions.IgnoreCase);
            sql = tableRegex.Replace(sql, m =>
            {
                var tableName = m.Groups[2].Value;
                return $"{m.Groups[1].Value} [{_schema}].[{prefix}{tableName}]";
            });

            //OBJECT_ID
            var objectIdRegex = new Regex(@"OBJECT_ID\(\s*N'(\[?(\w+)\]?)'\s*\)", RegexOptions.IgnoreCase);
            sql = objectIdRegex.Replace(sql, m =>
            {
                var tableName = m.Groups[2].Value;
                return $"OBJECT_ID(N'[{_schema}].[{prefix}{tableName}]')";
            });

            // constraint
            var constraintRegex = new Regex(@"\bCONSTRAINT\s+\[?(PK|FK|UQ|AK|IX)_(\w+)\]?", RegexOptions.IgnoreCase);
            sql = constraintRegex.Replace(sql, m =>
            {
                var typePrefix = m.Groups[1].Value;
                var namePart = m.Groups[2].Value;

                string finalName = $"{typePrefix}_{prefix}{namePart}";
                if (finalName.Length > MaxConstraintLength)
                {
                    using var sha1 = System.Security.Cryptography.SHA1.Create();
                    var hash = BitConverter.ToString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(finalName)))
                        .Replace("-", "").Substring(0, 8);
                    finalName = $"{typePrefix}_{prefix}_{hash}";
                }
                return $"CONSTRAINT [{finalName}]";
            });

            // index
            var indexRegex = new Regex(@"\bCREATE\s+(UNIQUE\s+)?INDEX\s+\[?(\w+)\]?\s+ON\s+\[?(\w+)\]?", RegexOptions.IgnoreCase);
            sql = indexRegex.Replace(sql, m =>
            {
                var unique = m.Groups[1].Value;
                var indexName = m.Groups[2].Value;
                var tableName = m.Groups[3].Value;
                return $"CREATE {unique}INDEX [{prefix}{indexName}] ON [{_schema}].[{prefix}{tableName}]";
            });

            // EF Migrations table
            var migrationTableRegex = new Regex(@"\[__EFMigrationsHistory\]", RegexOptions.IgnoreCase);
            sql = migrationTableRegex.Replace(sql, $"[{_schema}].[{prefix}__EFMigrationsHistory]");

            return sql;
        }
    }

}
