using System.Data.Common;
using System.Text.RegularExpressions;

namespace OrchardCore.MultiTenant.Migration.Executor
{
    public class PostgreSqlMigrationExecutor : MigrationExecutor
    {
        private readonly string _schema;

        public PostgreSqlMigrationExecutor(DbConnection connection, string tablePrefix, IWebHostEnvironment env, string schema = "public")
            : base(connection, tablePrefix, env)
        {
            _schema = string.IsNullOrWhiteSpace(schema) ? "public" : schema;
        }

        public override string Provider => "PostgreSQL";

        public override async Task EnsureHistoryTableAsync()
        {
            var tableName = $"{TablePrefix}__EFMigrationsHistory";
            string sql = $@"
            CREATE TABLE IF NOT EXISTS ""{_schema}"".""{tableName}""
            (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK{tableName}"" PRIMARY KEY (""MigrationId"")
            )";
            await ExecuteBatchAsync(sql);
        }

        public override string ApplyPrefix(string sql, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return sql;
            const int MaxConstraintLength = 64;
            // 1. table name
            var tableRegex = new Regex(@"(CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS|CREATE\s+TABLE|ALTER\s+TABLE|DROP\s+TABLE|INSERT\s+INTO|REFERENCES|UPDATE)\s+""?(\w+)""?", RegexOptions.IgnoreCase);
            sql = tableRegex.Replace(sql, m =>
            {
                var tableName = m.Groups[2].Value;
                return $"{m.Groups[1].Value} \"{_schema}\".\"{prefix}{tableName}\"";
            });

            // 2. constraint
            var constraintRegex = new Regex(@"\bCONSTRAINT\s+""?(PK|FK|UQ|AK|IX)_([^""]+)""?", RegexOptions.IgnoreCase);
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
                return $"CONSTRAINT \"{finalName}\"";
            });

            // 3. index
            var indexRegex = new Regex(@"\bCREATE\s+(UNIQUE\s+)?INDEX\s+""?(\w+)""?\s+ON\s+""?(\w+)""?", RegexOptions.IgnoreCase);
            sql = indexRegex.Replace(sql, m =>
            {
                var unique = m.Groups[1].Value;
                var indexName = m.Groups[2].Value;
                var tableName = m.Groups[3].Value;
                return $"CREATE {unique}INDEX \"{prefix}{indexName}\" ON \"{_schema}\".\"{prefix}{tableName}\"";
            });

            // 4. EF Migrations table
            var migrationTableRegex = new Regex("\"__EFMigrationsHistory\"", RegexOptions.IgnoreCase);
            sql = migrationTableRegex.Replace(sql, $"\"{_schema}\".\"{prefix}__EFMigrationsHistory\"");

            return sql;
        }
    }

}
