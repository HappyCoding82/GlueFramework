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
        protected readonly IDataTablePrefixProvider? _dataTablePrefixProvider;

        public bool HasActiveTransaction => AmbientTransactionContext.CurrentTransaction != null;

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

        protected DbSessionScope OpenDbSessionScope()
        {
            var ambientConn = AmbientTransactionContext.CurrentConnection;
            if (ambientConn != null)
                return new DbSessionScope(ambientConn, AmbientTransactionContext.CurrentTransaction, ownedConnection: null);

            var conn = _dataAccesser.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            return new DbSessionScope(conn, transaction: null, ownedConnection: conn as DbConnection);
        }

        protected readonly struct JoinQuerySessionScope : IDisposable
        {
            private readonly DbSessionScope _sessionScope;
            private readonly bool _disposeSessionScope;
            private readonly IDataTablePrefixProvider? _dataTablePrefixProvider;

            internal JoinQuerySessionScope(DbSessionScope sessionScope, JoinQuerySession session, bool disposeSessionScope)
            {
                _sessionScope = sessionScope;
                _disposeSessionScope = disposeSessionScope;
                Session = session;
                _dataTablePrefixProvider = session.TablePrefixProvider;
            }

            public JoinQuerySession Session { get; }

            public IDbConnection Connection => _sessionScope.Connection;

            public IDbTransaction? Transaction => _sessionScope.Transaction;

            public IRepository<T> GetRepository<T>() where T : class
            {
                return new Repository<T>(_sessionScope.Connection, _sessionScope.Transaction, _dataTablePrefixProvider);
            }

            public IRawSqlExecutor GetRawSqlExecutor()
            {
                return new RawSqlExecutor(_sessionScope.Connection, _sessionScope.Transaction);
            }

            public void Dispose()
            {
                if (_disposeSessionScope)
                    _sessionScope.Dispose();
            }
        }

        protected JoinQuerySessionScope OpenJoinQuerySessionScope()
        {
            var scope = OpenDbSessionScope();
            var session = JoinQuerySession.Wrap(scope.Connection, scope.Transaction, _dataTablePrefixProvider);
            return new JoinQuerySessionScope(scope, session, disposeSessionScope: true);
        }

        protected JoinQuerySessionScope OpenJoinQuerySessionScope(DbSessionScope sessionScope)
        {
            var session = JoinQuerySession.Wrap(sessionScope.Connection, sessionScope.Transaction, _dataTablePrefixProvider);
            return new JoinQuerySessionScope(sessionScope, session, disposeSessionScope: false);
        }

        protected async Task WithConnectionAsync(Func<IDbConnection, IDbTransaction?, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var ambientConn = AmbientTransactionContext.CurrentConnection;
            if (ambientConn != null)
            {
                await action(ambientConn, AmbientTransactionContext.CurrentTransaction);
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

            var ambientConn = AmbientTransactionContext.CurrentConnection;
            if (ambientConn != null)
                return await action(ambientConn, AmbientTransactionContext.CurrentTransaction);

            using var conn = _dataAccesser.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            return await action(conn, null);
        }

        public IDbTransaction BeginTransaction()
        {
            if (AmbientTransactionContext.HasActive)
                throw new InvalidOperationException("BeginTransaction called while an ambient transaction is already active.");

            TransactionScopeContext.Begin();

            var connection = _dataAccesser.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            var tx = connection.BeginTransaction();
            AmbientTransactionContext.Begin(connection, tx);
            return new ServiceTransaction(tx, connection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <returns></returns>
        public IPartitionRepository<Model> GetPartitionRepository<Model>() where Model : PartitionModelBase
        {
            var ambientConn = AmbientTransactionContext.CurrentConnection;
            if (ambientConn != null)
                return new PartitionedRepository<Model>(ambientConn, AmbientTransactionContext.CurrentTransaction, _dataTablePrefixProvider);

            return new PartitionedRepository<Model>(_dataAccesser.CreateConnection(),
                _dataTablePrefixProvider);
        }

        private sealed class ServiceTransaction : IDbTransaction
        {
            private readonly DbConnection _connection;
            private IDbTransaction? _inner;

            public ServiceTransaction(IDbTransaction inner, DbConnection connection)
            {
                _inner = inner;
                _connection = connection;
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

                try
                {
                    _connection.Close();
                }
                finally
                {
                    _connection.Dispose();
                    // Safety net: ensure ambient state never leaks if upstream cleanup paths are bypassed.
                    AmbientTransactionContext.Clear();
                }
            }
        }

        //public string CurrentUserId { get; set; }
    }
}
