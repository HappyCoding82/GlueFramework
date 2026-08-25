using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using GlueFramework.Core.ORM;
using System.Data;
using System.Linq.Expressions;
using static GlueFramework.Core.ORM.SqlBuilderFactory;

namespace GlueFramework.Core.UOW
{
    public class Repository<Model> : BaseRepository<Model>, IRepository<Model> where Model : class
    {
        DBTypes _dbType = DBTypes.None;
        ISqlBuilder<Model> _sqlBuilder = null;

        public Repository(IDbConnection db, IDataTablePrefixProvider dataTablePrefixProvider) : base(db)
        {
            InitSqlBuilder(DbConnection, dataTablePrefixProvider);
        }

        public Repository(IDbConnection db, IDbTransaction? dbTransaction, IDataTablePrefixProvider dataTablePrefixProvider) : base(db, dbTransaction)
        {
            InitSqlBuilder(DbConnection, dataTablePrefixProvider);
        }

        private void InitSqlBuilder(IDbConnection dbConn, IDataTablePrefixProvider dataTablePrefixProvider)
        {
            dbConn = DbConnectionUnwrapper.Unwrap(dbConn);

            var connectionType = dbConn.GetType().FullName;
            if (string.IsNullOrWhiteSpace(connectionType))
                connectionType = dbConn.GetType().Name;
            if (string.IsNullOrWhiteSpace(connectionType))
                connectionType = dbConn.ToString();
            if (_sqlBuilder == null)
                if (connectionType.Contains("Sqlite"))
                {
                    _sqlBuilder = SqlBuilderFactory.CreateInstance<Model>(DBTypes.SQLITE, 
                        dataTablePrefixProvider);
                }
                else
                {
                    if (connectionType.Contains("MySql", StringComparison.InvariantCultureIgnoreCase))
                        _sqlBuilder = SqlBuilderFactory.CreateInstance<Model>(DBTypes.MYSQL, 
                            dataTablePrefixProvider);
                    else if (connectionType.Contains("Npgsql", StringComparison.InvariantCultureIgnoreCase) ||
                             connectionType.Contains("Postgre", StringComparison.InvariantCultureIgnoreCase))
                        _sqlBuilder = SqlBuilderFactory.CreateInstance<Model>(DBTypes.POSTGRESQL,
                            dataTablePrefixProvider);
                    else
                        _sqlBuilder = SqlBuilderFactory.CreateInstance<Model>(DBTypes.SQLSERVER,
                            dataTablePrefixProvider);
                }
        }

        private ISqlBuilder<Model> CurrentSqlBuilder 
        {
            get
            {
                return _sqlBuilder;
            }
        }

        public async Task InsertAsync(Model data)
        {
            var insertSql = CurrentSqlBuilder.GetInsertSql();
            await ExecuteGetAffectAsync<Model>(insertSql, data);
        }

        public async Task<int> InsertPartialAsync(Action<PatchBuilder<Model>> patch)
        {
            if (patch == null)
                throw new ArgumentNullException(nameof(patch));

            var builder = new PatchBuilder<Model>();
            patch(builder);
            var cmd = CurrentSqlBuilder.BuildPartialInsertSql(builder.Changes);
            return await ExecuteGetAffectAsync<Model>(cmd.Key, cmd.Value);
        }

        public async Task InsertAsync(List<Model> models)
        {
            if (models == null || models.Count == 0)
                return;
            var insertSql = CurrentSqlBuilder.BuildBatchInsertSql(models);// GetInsertSql();
            await ExecuteGetAffectAsync<Model>(insertSql.Key,insertSql.Value);
        }

        public async Task<Model> InsertAndReturnAsync(Model data)
        {
            var insertSql = CurrentSqlBuilder.GetInsertAndReturnSql();
            return await InsertAndReturnAsync<Model>(insertSql, data);
        }

        public async Task<Model> UpdateAndReturnAsync(Model data)
        {
            var updateAndReturnSql = CurrentSqlBuilder.GetUpdateAndReturnSql();
            return await UpdateAndReturnAsync<Model>(updateAndReturnSql, data);
        }


        public async Task<int> UpdateAsync(List<Model> models)
        {
            var updateSql = CurrentSqlBuilder.GetUpdateSql();
            return await ExecuteGetAffectAsync<Model>(updateSql, models);
        }

        public async Task UpdateAsync(Model data)
        {
            var updateSql = CurrentSqlBuilder.GetUpdateSql();
            await ExecuteAsync<Model>(updateSql, data);
        }

        private async Task<int> UpdatePartialInternalAsync(Model keyModel, IReadOnlyDictionary<string, object?> changes)
        {
            var cmd = CurrentSqlBuilder.BuildPartialUpdateSql(keyModel, changes);
            return await ExecuteGetAffectAsync<Model>(cmd.Key, cmd.Value);
        }

        public async Task<int> UpdatePartialAsync(Model keyModel, Action<PatchBuilder<Model>> patch)
        {
            if (patch == null)
                throw new ArgumentNullException(nameof(patch));

            var builder = new PatchBuilder<Model>();
            patch(builder);
            return await UpdatePartialInternalAsync(keyModel, builder.Changes);
        }

        public async Task DeleteAsync(Model data)
        {
            var updateSql = CurrentSqlBuilder.GetDeleteByKey();
            await ExecuteGetAffectAsync<Model>(updateSql, data);
        }

        /// <summary>
        /// Delete multiple records by keys
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task DeleteAsync(List<Model> models)
        {
            var updateSql = CurrentSqlBuilder.GetDeleteByKey();
            await ExecuteGetAffectAsync<Model>(updateSql, models);
        }

        public async Task DeleteAsync(Expression<Func<Model, bool>> exp)
        {
            var sql = CurrentSqlBuilder.BuildDeleteSql(exp);
            await ExecuteGetAffectAsync<Model>(sql.Key, sql.Value);
        }

        public async Task<Model> GetSingleOrDefaultByKeyAsync(Model data)
        {
            return await GetSingleOrDefaultByKeyAsync(data, QueryLockType.None);
        }

        public async Task<Model> GetSingleOrDefaultByKeyAsync(Model data, QueryLockType lockType)
        {
            var getByKeyCmd = CurrentSqlBuilder.GetSelectByKeySql(lockType);
            return await QuerySingleOrDefaultAsync(getByKeyCmd, data);
        }

        public async Task<Model> GetByKeyAsync(Model data)
        {
            return await GetByKeyAsync(data, QueryLockType.None);
        }

        public async Task<Model> GetByKeyAsync(Model data, QueryLockType lockType)
        {
            var getByKeyCmd = CurrentSqlBuilder.GetSelectByKeySql(lockType);
            return await QuerySingleAsync(getByKeyCmd, data);
        }

        public async Task<IEnumerable<Model>> GetAllAsync()
        {
            return await GetAllAsync(QueryLockType.None);
        }

        public async Task<IEnumerable<Model>> GetAllAsync(QueryLockType lockType)
        {
            var query = CurrentSqlBuilder.GetSelectAllSql(lockType);
            return await QueryAsync<Model>(query, null);
        }

        public async Task<IEnumerable<Model>> GetTopRecordsAsync(int number)
        {
            return await GetTopRecordsAsync(number, QueryLockType.None);
        }

        public async Task<IEnumerable<Model>> GetTopRecordsAsync(int number, QueryLockType lockType)
        {
            var getByKeyCmd = CurrentSqlBuilder.GetSelectTopRecordsSql(number, lockType);
            return await QueryAsync<Model>(getByKeyCmd, null);
        }

        public async Task<IEnumerable<Model>> QueryTopAsync(Expression<Func<Model, bool>> exp, int number)
        {
            return await QueryTopAsync(exp, number, QueryLockType.None);
        }

        public async Task<IEnumerable<Model>> QueryTopAsync(Expression<Func<Model, bool>> exp, int number, QueryLockType lockType)
        {
            var sql = CurrentSqlBuilder.BuildQueryTop(exp, number, lockType);
            return await QueryAsync(sql);
        }

        public async Task<IEnumerable<Model>> QueryAsync(Expression<Func<Model, bool>> expression)
        {
            return await QueryAsync(expression, QueryLockType.None);
        }

        public async Task<IEnumerable<Model>> QueryAsync(Expression<Func<Model, bool>> expression, QueryLockType lockType)
        {
            var sql = CurrentSqlBuilder.BuildQuery(expression, lockType);
            return await QueryAsync(sql);
        }

        public async Task<Model> FirstOrDefaultAsync(Expression<Func<Model, bool>> exp)
        {
            return await FirstOrDefaultAsync(exp, QueryLockType.None);
        }

        public async Task<Model> FirstOrDefaultAsync(Expression<Func<Model, bool>> exp, QueryLockType lockType)
        {
            var sql = CurrentSqlBuilder.BuildQueryTop(exp, 1, lockType);
            return await FirstOrDefaultAsync(sql);
        }

        public async Task<PagerResult<Model>> PagerSearchAsync(PagedFilterOptions<Model> opts)
        {
            var cmd = CurrentSqlBuilder.BuildQuery(opts);
            PagerResult<Model> rs = new PagerResult<Model>();
            using (var reader = await QueryMultipleAsync(cmd.Key, cmd.Value))
            {
                rs.TotalCount = reader.ReadFirst<int>();
                rs.Results = reader.Read<Model>();
            }
            return rs;
        }
    }

}
