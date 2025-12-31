using OrchardCore.MultiTenant.Migration.Helpers;
using System.Data.Common;

namespace OrchardCore.MultiTenant.Migration.Executor
{
    public abstract class MigrationExecutor
    {
        protected readonly DbConnection Connection;
        protected readonly string TablePrefix;
        private readonly string _logDirectory;

        protected MigrationExecutor(DbConnection connection, string tablePrefix, IWebHostEnvironment env)
        {
            Connection = connection;
            TablePrefix = tablePrefix;

            // 构建日志目录：App_Data/SqlLogs
            _logDirectory = Path.Combine(env.ContentRootPath, "App_Data", "SqlLogs");

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public abstract Task EnsureHistoryTableAsync();

        public async Task ExecuteBatchAsync(string sql)
        {
            try
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                await WriteLogAsync(e, sql);
                throw;
            }
        }

        private async Task WriteLogAsync(Exception e, string sql)
        {
            try
            {
                string logFile = Path.Combine(_logDirectory, $"SqlError_{DateTime.Now:yyyyMMdd}.log");
                string logContent = $@"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]
ErrorMessage: {e.Message}
SQL: {sql}
--------------------------------------------------";

                await File.AppendAllTextAsync(logFile, logContent);
            }
            catch
            {
                Console.WriteLine("Failed to write log file.");
            }
        }

        public abstract string ApplyPrefix(string sql, string prefix);

        public abstract string Provider { get; }
    }
}
