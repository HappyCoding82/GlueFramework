using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using GlueFramework.Core.ORM;
using static GlueFramework.Core.ORM.SqlBuilderFactory;
using System.Data;

namespace GlueFramework.Core.UOW
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Model"></typeparam>
    public class PartitionedRepository<Model> : BaseRepository<Model>, IPartitionRepository<Model> where Model : PartitionModelBase
    {
        protected IDbConnection _connection;
        DBTypes _dbType = DBTypes.None;
        ISqlBuilderPartition _sqlBuilder = null;

        public PartitionedRepository(IDbConnection db, IDataTablePrefixProvider dataTablePrefixProvider) : base(db)
        {
            InitSqlBuilder(DbConnection, dataTablePrefixProvider);
        }

        public PartitionedRepository(IDbConnection db, IDbTransaction? dbTransaction, IDataTablePrefixProvider dataTablePrefixProvider) : base(db, dbTransaction)
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
                    _sqlBuilder = SqlBuilderFactory.CreatePartitionInstance<Model>(DBTypes.SQLITE,
                        dataTablePrefixProvider);
                }
                else
                {
                    if (connectionType.Contains("MySql", StringComparison.InvariantCultureIgnoreCase))
                        _sqlBuilder = SqlBuilderFactory.CreatePartitionInstance<Model>(DBTypes.MYSQL, 
                            dataTablePrefixProvider);
                    else if (connectionType.Contains("Npgsql", StringComparison.InvariantCultureIgnoreCase) ||
                             connectionType.Contains("Postgre", StringComparison.InvariantCultureIgnoreCase))
                        _sqlBuilder = SqlBuilderFactory.CreatePartitionInstance<Model>(DBTypes.POSTGRESQL,
                            dataTablePrefixProvider);
                    else
                        _sqlBuilder = SqlBuilderFactory.CreatePartitionInstance<Model>(DBTypes.SQLSERVER,
                            dataTablePrefixProvider);
                }
        }

        private ISqlBuilderPartition CurrentSqlBuilder
        {
            get
            {
                return _sqlBuilder;
            }
        }

        public async Task InsertAsync(Model data)
        {
            var insertSql = CurrentSqlBuilder.GetInsertSql<Model>(data);
            await ExecuteGetAffectAsync<Model>(insertSql, data);
        }


        public async Task<Model> InsertAndReturnAsync(Model data)
        {
            var insertSql = CurrentSqlBuilder.GetInsertAndReturnSql<Model>(data);
            return await QuerySingleOrDefaultAsync(insertSql, data);
        }

        public async Task<Model> UpdateAndReturnAsync(Model data)
        {
            var updateAndReturnSql = CurrentSqlBuilder.GetUpdateAndReturnSql<Model>(data);
            return await QuerySingleOrDefaultAsync(updateAndReturnSql, data);
        }

        public async Task ExecuteCmdAsyn(string updateCmd, Model data)
        {
            await ExecuteAsync(updateCmd, data);
        }

        public async Task UpdateAsync(Model data)
        {
            var updateSql = CurrentSqlBuilder.GetUpdateSql<Model>(data);
            await ExecuteCmdAsyn(updateSql, data);
        }

        public async Task DeleteAsync(Model data)
        {
            var updateSql = CurrentSqlBuilder.GetDeleteByKey<Model>(data);
            await ExecuteCmdAsyn(updateSql, data);
        }

        private async Task<Model> GetSingleOrDefaultByCmdAsync(string getByKeyCmd, Model data)
        {
            return await QuerySingleOrDefaultAsync<Model>(getByKeyCmd, data);
        }
        public async Task<Model> GetSingleOrDefaultByKeyAsync(Model data)
        {
            var getByKeyCmd = CurrentSqlBuilder.GetSelectByKeySql<Model>(data);
            return await GetSingleOrDefaultByCmdAsync(getByKeyCmd, data);
        }
        public async Task<Model> GetByKeyAsync(Model data)
        {
            var getByKeyCmd = CurrentSqlBuilder.GetSelectByKeySql<Model>(data);
            return await QuerySingleAsync(getByKeyCmd, data);
        }
    }

}
