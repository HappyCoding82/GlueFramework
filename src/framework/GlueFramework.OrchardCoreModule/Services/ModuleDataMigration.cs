using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GlueFramework.OrchardCoreModule.Abstractions;
using OrchardCore.Data.Migration;
using OrchardCore.Environment.Shell;
using System.Text.RegularExpressions;
using YesSql;

namespace GlueFramework.OrchardCoreModule.Services
{
    [Obsolete()]
    public class ModuleDataMigration<T> : DataMigration,IModuleDataMigration<T>
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ShellSettings _shellSettings;
        protected readonly YesSql.ISession _yesSession;
        protected readonly ILogger<ModuleDataMigration<T>> _logger;

        public ModuleDataMigration(IHttpContextAccessor httpContextAccessor,
          //IShellSettingsManager shellSettingsManager,
          ShellSettings shellSettings,
          YesSql.ISession session,
          ILogger<ModuleDataMigration<T>> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _shellSettings = shellSettings;
            _yesSession = session;
            _logger = logger;
        }

        public async Task ExcuteSqlFile(YesSql.ISession session)
        {
            var conn = session.CurrentTransaction.Connection!;

            string fileContent = string.Empty;
            string connType = conn.GetType().FullName;
            bool isMySql = connType == "MySqlConnector.MySqlConnection";
            bool isMsSql = connType == "Microsoft.Data.SqlClient.SqlConnection";
            bool isSqlite = connType == "Microsoft.Data.Sqlite.SqliteConnection";
            if (isMySql)
                fileContent = EmbeddedResourceService.GetEmbeddedResourceContent<T>("initdb_mysql.sql", true);

            if (isSqlite)
                fileContent = EmbeddedResourceService.GetEmbeddedResourceContent<T>("initdb_sqlite.sql", true);

            if (isMsSql)
                fileContent = EmbeddedResourceService.GetEmbeddedResourceContent<T>("initdb_mssql.sql", true);

            var commands = fileContent.Split(new[] { isMsSql ? "\r\nGO" : ";" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var command in commands)
            {
                if (command.Trim().Length > 0)
                {
                    var regMatchTable = isMySql ? @"(CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS|CREATE\s+TABLE|INSERT\s+INTO)\s+`?(\w+)`?" :
                        @"(CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS|CREATE\s+TABLE|INSERT\s+INTO)\s+\[?(\w+)\]?\s+";
                    string modifiedSql = Regex.Replace(command, regMatchTable, match =>
                    {
                        string tableName = match.Groups[2].Value;
                        string modifiedTableName = session.Store.Configuration.TablePrefix + tableName;
                        return match.Groups[1].Value + (isMySql ? " `" : "[") + modifiedTableName + (isMySql ? "`" : "]");
                    });
               
                    //CONSTRAINT `FK_JobPosition_ProfessionalCategory` FOREIGN KEY (`ProfessionalCategoryId`) REFERENCES `ProfessionalCategory` (`Id`)"
                    //Replace Constraint
                    regMatchTable = isMySql ? @"(CONSTRAINT\s+)`?(\w+)`?(\s+)" : @"(CONSTRAINT\s+)[?(\w+)]?(\s+)";
                    modifiedSql = Regex.Replace(modifiedSql, regMatchTable, match =>
                    {
                        string tableName = match.Groups[2].Value;
                        string modifiedTableName = session.Store.Configuration.TablePrefix + tableName;
                        return match.Groups[1].Value + (isMySql ? " `" : "[") + modifiedTableName + (isMySql ? "`" : "]") + (match.Groups.Count > 3? match.Groups[3].Value : "");
                    });

                    //regMatchTable = isMySql ? @"(FOREIGN\s+KEY\s+\()`?(\w+)`?(\))" : @"(FOREIGN\s+KEY\s+\()[?(\w+)]?(\))";
                    //modifiedSql = Regex.Replace(modifiedSql, regMatchTable, match =>
                    //{
                    //    string tableName = match.Groups[2].Value;
                    //    string modifiedTableName = session.Store.Configuration.TablePrefix + tableName;
                    //    return match.Groups[1].Value + (isMySql ? " `" : "[") + modifiedTableName + (isMySql ? "`" : "]") + (match.Groups.Count > 3 ? match.Groups[3].Value : "");
                    //});

                    regMatchTable = isMySql ? @"(REFERENCES\s+)`?(\w+)`?\s+" : @"(REFERENCES\s+)[?(\w+)]?\s+";
                    modifiedSql = Regex.Replace(modifiedSql, regMatchTable, match =>
                    {
                        string tableName = match.Groups[2].Value;
                        string modifiedTableName = session.Store.Configuration.TablePrefix + tableName;
                        return match.Groups[1].Value + (isMySql ? " `" : "[") + modifiedTableName + (isMySql ? "`" : "]");
                    });
                    //command = command.Replace("")
                    Console.WriteLine($"Executing command: {modifiedSql}");
                    conn.Execute(modifiedSql, null, session.CurrentTransaction);
                }
                else
                {
                }
            }
        }

        public virtual async Task<int> CreateAsync()
        {
            try
            {
                await ExcuteSqlFile(_yesSession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Datamigration error");
            }
            return 1;
        }

    }
}
