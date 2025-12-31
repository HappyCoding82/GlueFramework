using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.UOW;
using System.Data;

namespace GlueFramework.Core.DataLayer
{
    public abstract class DALBase
    {
        public DALBase(IDbConnectionAccessor dbConnectionAccessor)
        {
            _dbConnectionAccessor = dbConnectionAccessor;
        }
        public DALBase(IDbConnectionAccessor dbConnectionAccessor, IDataTablePrefixProvider tableNamePrefixProvider)
        {
            _dbConnectionAccessor = dbConnectionAccessor;
            _tableNamePrefixProvider = tableNamePrefixProvider;
        }

        protected IDbConnectionAccessor _dbConnectionAccessor;
        protected IDataTablePrefixProvider _tableNamePrefixProvider;
        protected IDbConnection DbConnection
        {
            get
            {
                var dbConn = _dbConnectionAccessor.CreateConnection();

                return dbConn;
            }
        }

        protected IRepository<T> GetRepository<T> () where T : class 
        {
            return new Repository<T>(_dbConnectionAccessor.CreateConnection(),
                _tableNamePrefixProvider);
        }

        public async Task<IEnumerable<Model>> QueryAsync<Model>(string sql)
        {
            return await DbConnection.QueryAsync<Model>(sql, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <returns></returns>
        protected IPartitionRepository<Model> GetPartitionRepository<Model>() where Model : PartitionModelBase
        {
            return new PartitionedRepository<Model>(_dbConnectionAccessor.CreateConnection(),
                _tableNamePrefixProvider);
        }
    }
}
