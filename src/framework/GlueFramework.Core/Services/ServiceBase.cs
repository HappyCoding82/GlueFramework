using GlueFramework.Core.Abstractions;
using GlueFramework.Core.ORM;
using GlueFramework.Core.UOW;
using System.Data;
using System.Data.Common;

namespace GlueFramework.Core.Services
{
    public class ServiceBase : IServiceBase, ICacheKeyPrefixProvider
    {
        private IDbConnectionAccessor _dataAccesser;
        private readonly IDataTablePrefixProvider? _dataTablePrefixProvider;

        private DbConnection? _transactionConnection;
        private IDbTransaction? _transaction;
        private ServiceTransaction? _serviceTransaction;

        public bool HasActiveTransaction => _transaction != null;

        public string? CacheKeyPrefix => _dataTablePrefixProvider?.Prefix;

        public string? TransactionKeyPrefix => CacheKeyPrefix;

        public ServiceBase(IDbConnectionAccessor dbConnAccessor, IDataTablePrefixProvider dataTablePrefixProvider)
        {
            _dataAccesser = dbConnAccessor;
            _dataTablePrefixProvider = dataTablePrefixProvider;
        }

        public ServiceBase(IDbConnectionAccessor dbConnAccessor)
        {
            _dataAccesser = dbConnAccessor;
        }

        protected readonly struct ConnectionScope : IDisposable
        {
            private readonly DbConnection? _ownedConnection;

            internal ConnectionScope(IDbConnection connection, IDbTransaction? transaction, DbConnection? ownedConnection)
            {
                Connection = connection;
                Transaction = transaction;
                _ownedConnection = ownedConnection;
            }

            public IDbConnection Connection { get; }

            public IDbTransaction? Transaction { get; }

            public void Dispose()
            {
                if (_ownedConnection != null)
                {
                    _ownedConnection.Close();
                    _ownedConnection.Dispose();
                }
            }
        }

        protected ConnectionScope OpenConnectionScope()
        {
            if (_transactionConnection != null)
                return new ConnectionScope(_transactionConnection, _transaction, ownedConnection: null);

            var conn = _dataAccesser.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            return new ConnectionScope(conn, transaction: null, ownedConnection: conn as DbConnection);
        }

        protected readonly struct JoinQuerySessionScope : IDisposable
        {
            private readonly ConnectionScope _connectionScope;
            private readonly IDataTablePrefixProvider? _dataTablePrefixProvider;

            internal JoinQuerySessionScope(ConnectionScope connectionScope, JoinQuerySession session)
            {
                _connectionScope = connectionScope;
                Session = session;
                _dataTablePrefixProvider = session.TablePrefixProvider;
            }

            public JoinQuerySession Session { get; }

            public IDbConnection Connection => _connectionScope.Connection;

            public IDbTransaction? Transaction => _connectionScope.Transaction;

            public IRepository<T> GetRepository<T>() where T : class
            {
                return new Repository<T>(_connectionScope.Connection, _connectionScope.Transaction, _dataTablePrefixProvider);
            }

            public IRawSqlExecutor GetRawSqlExecutor()
            {
                return new RawSqlExecutor(_connectionScope.Connection, _connectionScope.Transaction);
            }

            public void Dispose()
            {
                _connectionScope.Dispose();
            }
        }

        protected JoinQuerySessionScope OpenJoinQuerySessionScope()
        {
            var scope = OpenConnectionScope();
            var session = JoinQuerySession.Wrap(scope.Connection, scope.Transaction, _dataTablePrefixProvider);
            return new JoinQuerySessionScope(scope, session);
        }

        protected async Task WithConnectionAsync(Func<IDbConnection, IDbTransaction?, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (_transactionConnection != null)
            {
                await action(_transactionConnection, _transaction);
                return;
            }

            using var conn = _dataAccesser.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            await action(conn, null);
        }

        protected async Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, IDbTransaction?, Task<TResult>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (_transactionConnection != null)
                return await action(_transactionConnection, _transaction);

            using var conn = _dataAccesser.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            return await action(conn, null);
        }

        public IDbTransaction BeginTransaction()
        {
            if (_serviceTransaction != null)
                return _serviceTransaction;

            TransactionScopeContext.Begin();

            _transactionConnection = _dataAccesser.CreateConnection();

            if (_transactionConnection.State == ConnectionState.Closed)
                _transactionConnection.Open();

            _transaction = _transactionConnection.BeginTransaction();
            _serviceTransaction = new ServiceTransaction(this, _transaction);
            return _serviceTransaction;
        }

        private void ClearTransaction()
        {
            _transaction = null;
            _serviceTransaction = null;
            if (_transactionConnection != null)
            {
                _transactionConnection.Close();
                _transactionConnection.Dispose();
            }
            _transactionConnection = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <returns></returns>
        public IPartitionRepository<Model> GetPartitionRepository<Model>() where Model : PartitionModelBase
        {
            if (_transactionConnection != null)
                return new PartitionedRepository<Model>(_transactionConnection, _transaction, _dataTablePrefixProvider);

            return new PartitionedRepository<Model>(_dataAccesser.CreateConnection(), 
                _dataTablePrefixProvider);
        }

        private sealed class ServiceTransaction : IDbTransaction
        {
            private readonly ServiceBase _service;
            private IDbTransaction? _inner;

            public ServiceTransaction(ServiceBase service, IDbTransaction inner)
            {
                _service = service;
                _inner = inner;
            }

            public IDbConnection Connection => _inner!.Connection!;

            public IsolationLevel IsolationLevel => _inner!.IsolationLevel;

            public void Commit()
            {
                _inner!.Commit();
                TransactionScopeContext.Commit();
            }

            public void Rollback()
            {
                _inner!.Rollback();
                TransactionScopeContext.Rollback();
            }

            public void Dispose()
            {
                if (_inner != null)
                {
                    _inner.Dispose();
                    _inner = null;
                }

                _service.ClearTransaction();
            }
        }

        //public string CurrentUserId { get; set; }
    }
}
