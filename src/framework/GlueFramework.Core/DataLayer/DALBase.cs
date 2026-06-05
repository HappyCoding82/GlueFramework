using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.ORM;
using GlueFramework.Core.UOW;
using System.Data;

namespace GlueFramework.Core.DataLayer
{
    public abstract class DALBase:IDALBase
    {

        protected readonly IDbSession _session;

        public DALBase(IDbSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public DALBase(IDbSession session, IDataTablePrefixProvider tableNamePrefixProvider)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _tableNamePrefixProvider = tableNamePrefixProvider;
        }


        protected IDataTablePrefixProvider _tableNamePrefixProvider;
        protected IDbConnection DbConnection
        {
            get
            {
                return _session.Connection;
            }
        }

        protected IRepository<T> GetRepository<T>() where T : class
        {
            return new Repository<T>(_session.Connection, _session.Transaction, _tableNamePrefixProvider);
        }

        public async Task<IEnumerable<Model>> QueryAsync<Model>(string sql)
        {
            return await DbConnection.QueryAsync<Model>(sql, null, _session.Transaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <returns></returns>
        protected IPartitionRepository<Model> GetPartitionRepository<Model>() where Model : PartitionModelBase
        {
            return new PartitionedRepository<Model>(_session.Connection, _session.Transaction, _tableNamePrefixProvider);
        }
    }

}
