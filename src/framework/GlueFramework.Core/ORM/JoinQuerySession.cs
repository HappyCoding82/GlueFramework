using Dapper;
using GlueFramework.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.Core.ORM
{
    public sealed class JoinQuerySession
    {
        private int _inFlight;

        private JoinQuerySession(IDbConnection connection, IDbTransaction? transaction, IDataTablePrefixProvider? tablePrefixProvider)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction;
            TablePrefixProvider = tablePrefixProvider;
            SessionId = Guid.NewGuid();
        }

        public Guid SessionId { get; }

        public IDbConnection Connection { get; }

        public IDbTransaction? Transaction { get; }

        public IDataTablePrefixProvider? TablePrefixProvider { get; }

        public static JoinQuerySession Wrap(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null)
        {
            return new JoinQuerySession(connection, transaction, tablePrefixProvider);
        }

        internal async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0)
            {
                throw new InvalidOperationException(
                    $"JoinQuerySession is already executing a query. SessionId={SessionId}. " +
                    "Do not share a session/connection across concurrent operations. " +
                    "If you need parallel queries, create multiple sessions (different connections), one per task.");
            }

            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _inFlight, 0);
            }
        }

        public SessionJoinQueryBuilder<T1> From<T1>() where T1 : class
        {
            return new SessionJoinQueryBuilder<T1>(this, JoinQuery.From<T1>());
        }
    }

    public sealed class SessionJoinQueryBuilder<T1> where T1 : class
    {
        private readonly JoinQuerySession _session;
        private readonly JoinQueryBuilder<T1> _inner;

        internal SessionJoinQueryBuilder(JoinQuerySession session, JoinQueryBuilder<T1> inner)
        {
            _session = session;
            _inner = inner;
        }

        public SessionJoinQueryBuilder<T1, T2> Join<T2>(Expression<Func<T1, T2, bool>> on, JoinType joinType = JoinType.Inner) where T2 : class
        {
            return new SessionJoinQueryBuilder<T1, T2>(_session, _inner.Join(on, joinType));
        }
    }

    public sealed class SessionJoinQueryBuilder<T1, T2>
        where T1 : class where T2 : class
    {
        private readonly JoinQuerySession _session;
        private readonly JoinQueryBuilder<T1, T2> _inner;

        internal SessionJoinQueryBuilder(JoinQuerySession session, JoinQueryBuilder<T1, T2> inner)
        {
            _session = session;
            _inner = inner;
        }

        public SessionJoinQueryBuilder<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate)
        {
            _inner.Where(predicate);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2> OrderBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
        {
            _inner.OrderBy(keySelector, desc);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2> ThenBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
        {
            _inner.ThenBy(keySelector, desc);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2> Page(int skip, int take)
        {
            _inner.Page(skip, take);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3> Join<T3>(Expression<Func<T1, T2, T3, bool>> on, JoinType joinType = JoinType.Inner) where T3 : class
        {
            return new SessionJoinQueryBuilder<T1, T2, T3>(_session, _inner.Join(on, joinType));
        }

        public SessionJoinQuerySelectBuilder<TDto> Select<TDto>(Action<SelectMap<T1, T2, TDto>> map) where TDto : class
        {
            return new SessionJoinQuerySelectBuilder<TDto>(_session, _inner.Select(map));
        }

        public SessionJoinQuerySelectBuilder<TDto> Select<TDto>(Expression<Func<T1, T2, TDto>> projection) where TDto : class
        {
            return new SessionJoinQuerySelectBuilder<TDto>(_session, _inner.Select(projection));
        }

        public sealed class SessionJoinQuerySelectBuilder<TDto> where TDto : class
        {
            private readonly JoinQuerySession _session;
            private readonly JoinQuerySelectBuilder<T1, T2, TDto> _inner;

            internal SessionJoinQuerySelectBuilder(JoinQuerySession session, JoinQuerySelectBuilder<T1, T2, TDto> inner)
            {
                _session = session;
                _inner = inner;
            }

            public SessionJoinQuerySelectBuilder<TDto> Where(Expression<Func<T1, T2, bool>> predicate)
            {
                _inner.Where(predicate);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> OrderBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
            {
                _inner.OrderBy(keySelector, desc);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> ThenBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
            {
                _inner.ThenBy(keySelector, desc);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> Page(int skip, int take)
            {
                _inner.Page(skip, take);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> Distinct(bool distinct = true)
            {
                _inner.Distinct(distinct);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> GroupBy(Expression<Func<T1, T2, object>> keySelector)
            {
                _inner.GroupBy(keySelector);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> Having(Expression<Func<T1, T2, bool>> predicate)
            {
                _inner.Having(predicate);
                return this;
            }

            public Task<(string, DynamicParameters)> PrintToListSqlAsync()
            {
                return _session.ExecuteAsync(() =>
                    _inner.PrintToListSqlAsync(
                        _session.Connection,
                        transaction: _session.Transaction,
                        tablePrefixProvider: _session.TablePrefixProvider));
            }

            public Task<IReadOnlyList<TDto>> ToListAsync()
            {
                return _session.ExecuteAsync(async () =>
                {
                    var rows = await _inner.ToListAsync(
                        _session.Connection,
                        transaction: _session.Transaction,
                        tablePrefixProvider: _session.TablePrefixProvider);
                    return (IReadOnlyList<TDto>)rows;
                });
            }
        }
    }

    public sealed class SessionJoinQueryBuilder<T1, T2, T3>
        where T1 : class where T2 : class where T3 : class
    {
        private readonly JoinQuerySession _session;
        private readonly JoinQueryBuilder<T1, T2, T3> _inner;

        internal SessionJoinQueryBuilder(JoinQuerySession session, JoinQueryBuilder<T1, T2, T3> inner)
        {
            _session = session;
            _inner = inner;
        }

        public SessionJoinQueryBuilder<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate)
        {
            _inner.Where(predicate);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3> OrderBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
        {
            _inner.OrderBy(keySelector, desc);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3> ThenBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
        {
            _inner.ThenBy(keySelector, desc);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3> Page(int skip, int take)
        {
            _inner.Page(skip, take);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3, T4> Join<T4>(Expression<Func<T1, T2, T3, T4, bool>> on, JoinType joinType = JoinType.Inner) where T4 : class
        {
            return new SessionJoinQueryBuilder<T1, T2, T3, T4>(_session, _inner.Join(on, joinType));
        }

        public SessionJoinQuerySelectBuilder<TDto> Select<TDto>(Action<SelectMap<T1, T2, T3, TDto>> map) where TDto : class
        {
            return new SessionJoinQuerySelectBuilder<TDto>(_session, _inner.Select(map));
        }

        public SessionJoinQuerySelectBuilder<TDto> Select<TDto>(Expression<Func<T1, T2, T3, TDto>> projection) where TDto : class
        {
            return new SessionJoinQuerySelectBuilder<TDto>(_session, _inner.Select(projection));
        }

        public sealed class SessionJoinQuerySelectBuilder<TDto> where TDto : class
        {
            private readonly JoinQuerySession _session;
            private readonly JoinQuerySelectBuilder<T1, T2, T3, TDto> _inner;

            internal SessionJoinQuerySelectBuilder(JoinQuerySession session, JoinQuerySelectBuilder<T1, T2, T3, TDto> inner)
            {
                _session = session;
                _inner = inner;
            }

            public SessionJoinQuerySelectBuilder<TDto> Where(Expression<Func<T1, T2, T3, bool>> predicate)
            {
                _inner.Where(predicate);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> OrderBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
            {
                _inner.OrderBy(keySelector, desc);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> ThenBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
            {
                _inner.ThenBy(keySelector, desc);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> Page(int skip, int take)
            {
                _inner.Page(skip, take);
                return this;
            }

            public Task<IReadOnlyList<TDto>> ToListAsync()
            {

                return _session.ExecuteAsync(async () =>
                {
                    var rows = await _inner.ToListAsync(
                        _session.Connection,
                        transaction: _session.Transaction,
                        tablePrefixProvider: _session.TablePrefixProvider);
                    return (IReadOnlyList<TDto>)rows;
                });
            }

            public Task<(string, DynamicParameters)> PrintToListSqlAsync()
            {
                return _session.ExecuteAsync(async () =>
                {
                    return await _inner.PrintToListSqlAsync(
                    _session.Connection,
                        transaction: _session.Transaction,
                        tablePrefixProvider: _session.TablePrefixProvider);
                });
            }
        }
    }

    public sealed class SessionJoinQueryBuilder<T1, T2, T3, T4>
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        private readonly JoinQuerySession _session;
        private readonly JoinQueryBuilder<T1, T2, T3, T4> _inner;

        internal SessionJoinQueryBuilder(JoinQuerySession session, JoinQueryBuilder<T1, T2, T3, T4> inner)
        {
            _session = session;
            _inner = inner;
        }

        public SessionJoinQueryBuilder<T1, T2, T3, T4> Where(Expression<Func<T1, T2, T3, T4, bool>> predicate)
        {
            _inner.Where(predicate);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3, T4> OrderBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
        {
            _inner.OrderBy(keySelector, desc);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3, T4> ThenBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
        {
            _inner.ThenBy(keySelector, desc);
            return this;
        }

        public SessionJoinQueryBuilder<T1, T2, T3, T4> Page(int skip, int take)
        {
            _inner.Page(skip, take);
            return this;
        }

        public SessionJoinQuerySelectBuilder<TDto> Select<TDto>(Action<SelectMap<T1, T2, T3, T4, TDto>> map) where TDto : class
        {
            return new SessionJoinQuerySelectBuilder<TDto>(_session, _inner.Select(map));
        }

        public SessionJoinQuerySelectBuilder<TDto> Select<TDto>(Expression<Func<T1, T2, T3, T4, TDto>> projection) where TDto : class
        {
            return new SessionJoinQuerySelectBuilder<TDto>(_session, _inner.Select(projection));
        }

        public sealed class SessionJoinQuerySelectBuilder<TDto> where TDto : class
        {
            private readonly JoinQuerySession _session;
            private readonly JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> _inner;

            internal SessionJoinQuerySelectBuilder(JoinQuerySession session, JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> inner)
            {
                _session = session;
                _inner = inner;
            }

            public SessionJoinQuerySelectBuilder<TDto> Where(Expression<Func<T1, T2, T3, T4, bool>> predicate)
            {
                _inner.Where(predicate);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> OrderBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
            {
                _inner.OrderBy(keySelector, desc);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> ThenBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
            {
                _inner.ThenBy(keySelector, desc);
                return this;
            }

            public SessionJoinQuerySelectBuilder<TDto> Page(int skip, int take)
            {
                _inner.Page(skip, take);
                return this;
            }

            public Task<IReadOnlyList<TDto>> ToListAsync()
            {
                return _session.ExecuteAsync(async () =>
                {
                    var rows = await _inner.ToListAsync(
                        _session.Connection,
                        transaction: _session.Transaction,
                        tablePrefixProvider: _session.TablePrefixProvider);
                    return (IReadOnlyList<TDto>)rows;
                });
            }
        }
    }
}
