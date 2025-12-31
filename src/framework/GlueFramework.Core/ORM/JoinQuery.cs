using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlueFramework.Core.ORM
{
    internal static class JoinQueryTableName
    {
        public static string WithPrefix(string tableName, IDataTablePrefixProvider? prefixProvider)
        {
            if (prefixProvider == null)
                return tableName;
            return prefixProvider.Prefix + tableName;
        }

        public static string WithSchemaAndPrefix(string tableName, IDataTablePrefixProvider? prefixProvider)
        {
            if (prefixProvider == null)
                return tableName;

            var prefixed = prefixProvider.Prefix + tableName;
            if (prefixProvider is ITenantTableSettingsProvider ts && !string.IsNullOrWhiteSpace(ts.Schema))
                return ts.Schema + "." + prefixed;

            return prefixed;
        }
    }

    internal interface IJoinQueryDialect
    {
        string QuoteIdentifier(string identifier);

        string LikeOperator { get; }

        void AppendPaging(StringBuilder sb, DynamicParameters parameters, int skip, int take, bool hasOrderBy);

        string NotBoolean(string expression);
    }

    internal static class JoinQueryDialectFactory
    {
        public static IJoinQueryDialect FromConnection(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            connection = DbConnectionUnwrapper.Unwrap(connection);

            var t = connection.GetType();
            var name = t.FullName ?? t.Name;

            if (name.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Postgre", StringComparison.OrdinalIgnoreCase) >= 0)
                return new PostgreSqlJoinQueryDialect();

            return new SqlServerJoinQueryDialect();
        }
    }

    internal sealed class PostgreSqlJoinQueryDialect : IJoinQueryDialect
    {
        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier is required.", nameof(identifier));
            if (identifier.Contains('.'))
            {
                var parts = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries);
                return string.Join(".", parts.Select(QuoteIdentifier));
            }
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        public string LikeOperator => "ILIKE";

        public void AppendPaging(StringBuilder sb, DynamicParameters parameters, int skip, int take, bool hasOrderBy)
        {
            parameters.Add("take", take);
            parameters.Add("skip", skip);
            sb.Append(" LIMIT @take OFFSET @skip");
        }

        public string NotBoolean(string expression)
        => $"NOT ({expression})";
    }

    internal sealed class SqlServerJoinQueryDialect : IJoinQueryDialect
    {
        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier is required.", nameof(identifier));
            if (identifier.Contains('.'))
            {
                var parts = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries);
                return string.Join(".", parts.Select(QuoteIdentifier));
            }
            return "[" + identifier.Replace("]", "]]" ) + "]";
        }

        public string LikeOperator => "LIKE";

        public void AppendPaging(StringBuilder sb, DynamicParameters parameters, int skip, int take, bool hasOrderBy)
        {
            if (!hasOrderBy)
                throw new InvalidOperationException("SQL Server paging requires ORDER BY.");

            parameters.Add("skip", skip);
            parameters.Add("take", take);
            sb.Append(" OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
        }

        public string NotBoolean(string expression)
        => $"({expression} = 0)";
    }

    public enum JoinType
    {
        Inner = 0,
        Left = 1
    }

    public static class JoinQuery
    {
        public static JoinQueryBuilder<T1> From<T1>(string alias) where T1 : class
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Alias is required.", nameof(alias));

            return new JoinQueryBuilder<T1>(alias);
        }

        public static JoinQueryBuilder<T1> From<T1>() where T1 : class
        {
            return new JoinQueryBuilder<T1>("t1");
        }
    }

    public sealed class JoinQueryBuilder<T1> where T1 : class
    {
        private readonly string _a1;

        internal JoinQueryBuilder(string a1)
        {
            _a1 = a1;
        }

        public JoinQueryBuilder<T1, T2> Join<T2>(string alias2, Expression<Func<T1, T2, bool>> on, JoinType joinType = JoinType.Inner) where T2 : class
        {
            if (string.IsNullOrWhiteSpace(alias2))
                throw new ArgumentException("Alias is required.", nameof(alias2));
            if (on == null)
                throw new ArgumentNullException(nameof(on));

            return new JoinQueryBuilder<T1, T2>(_a1, alias2, on, joinType);
        }

        public JoinQueryBuilder<T1, T2> Join<T2>(Expression<Func<T1, T2, bool>> on, JoinType joinType = JoinType.Inner) where T2 : class
        {
            if (on == null)
                throw new ArgumentNullException(nameof(on));

            return new JoinQueryBuilder<T1, T2>(_a1, "t2", on, joinType);
        }
    }

    public sealed class JoinQueryBuilder<T1, T2> where T1 : class where T2 : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly Expression<Func<T1, T2, bool>> _on;
        private readonly JoinType _joinType;

        private Expression<Func<T1, T2, bool>>? _where;
        private readonly List<(LambdaExpression keySelector, bool desc)> _orderBys = new();
        private (int skip, int take)? _page;

        private bool _distinct;
        private LambdaExpression? _groupBy;
        private Expression<Func<T1, T2, bool>>? _having;

        internal JoinQueryBuilder(string a1, string a2, Expression<Func<T1, T2, bool>> on, JoinType joinType)
        {
            _a1 = a1;
            _a2 = a2;
            _on = on;
            _joinType = joinType;
        }

        public JoinQueryBuilder<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate)
        {
            _where = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public JoinQueryBuilder<T1, T2> OrderBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _orderBys.Clear();
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryBuilder<T1, T2> ThenBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (_orderBys.Count == 0)
                throw new InvalidOperationException("OrderBy must be called before ThenBy.");
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryBuilder<T1, T2> Page(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take <= 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            _page = (skip, take);
            return this;
        }

        internal void SetDistinct(bool distinct) => _distinct = distinct;
        internal void SetGroupBy(LambdaExpression keySelector) => _groupBy = keySelector;
        internal void SetHaving(Expression<Func<T1, T2, bool>> predicate) => _having = predicate;

        public JoinQueryBuilder<T1, T2, T3> Join<T3>(
            string alias3,
            Expression<Func<T1, T2, T3, bool>> on,
            JoinType joinType = JoinType.Inner) where T3 : class
        {
            if (string.IsNullOrWhiteSpace(alias3))
                throw new ArgumentException("Alias is required.", nameof(alias3));
            if (on == null)
                throw new ArgumentNullException(nameof(on));

            return new JoinQueryBuilder<T1, T2, T3>(
                _a1,
                _a2,
                _on,
                _joinType,
                alias3,
                on,
                joinType);
        }

        public JoinQueryBuilder<T1, T2, T3> Join<T3>(
            Expression<Func<T1, T2, T3, bool>> on,
            JoinType joinType = JoinType.Inner) where T3 : class
        {
            if (on == null)
                throw new ArgumentNullException(nameof(on));

            return new JoinQueryBuilder<T1, T2, T3>(
                _a1,
                _a2,
                _on,
                _joinType,
                "t3",
                on,
                joinType);
        }

        public JoinQueryViewBuilder<T1, T2, TView> UseView<TView>(Action<ViewMap<T1, T2, TView>> map) where TView : class
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var vm = new ViewMap<T1, T2, TView>(_a1, _a2);
            map(vm);
            vm.Freeze();
            return new JoinQueryViewBuilder<T1, T2, TView>(this, vm);
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> Select<TDto>(Action<SelectMap<T1, T2, TDto>> map) where TDto : class
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var sm = new SelectMap<T1, T2, TDto>(_a1, _a2);
            map(sm);
            sm.Freeze();
            return new JoinQuerySelectBuilder<T1, T2, TDto>(this, sm);
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> Select<TDto>(Expression<Func<T1, T2, TDto>> projection) where TDto : class
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            var sm = new SelectMap<T1, T2, TDto>(_a1, _a2);
            JoinQueryProjectionSelectMapBuilder.Build(sm, projection);
            sm.Freeze();
            return new JoinQuerySelectBuilder<T1, T2, TDto>(this, sm);
        }

        internal (string a1, string a2) Aliases => (_a1, _a2);
        internal Expression<Func<T1, T2, bool>> On => _on;
        internal JoinType JoinType => _joinType;
        internal Expression<Func<T1, T2, bool>>? WhereExpr => _where;
        internal IReadOnlyList<(LambdaExpression keySelector, bool desc)> OrderBys => _orderBys;
        internal (int skip, int take)? PageSpec => _page;

        internal bool Distinct => _distinct;
        internal LambdaExpression? GroupByExpr => _groupBy;
        internal Expression<Func<T1, T2, bool>>? HavingExpr => _having;
    }

    internal static class JoinQueryProjectionSelectMapBuilder
    {
        internal static void Build<T1, T2, TDto>(SelectMap<T1, T2, TDto> sm, Expression<Func<T1, T2, TDto>> projection)
            where T1 : class where T2 : class where TDto : class
        {
            var bindings = GetBindings(projection.Body);
            foreach (var (dtoPropName, rhs) in bindings)
            {
                var dtoParam = Expression.Parameter(typeof(TDto), "dto");
                var dtoMember = Expression.PropertyOrField(dtoParam, dtoPropName);
                var dtoPropertyExpr = Expression.Lambda<Func<TDto, object>>(
                    Expression.Convert(dtoMember, typeof(object)),
                    dtoParam);

                var sourceExpr = Expression.Lambda<Func<T1, T2, object>>(
                    Expression.Convert(rhs, typeof(object)),
                    projection.Parameters);

                sm.Column(dtoPropertyExpr, sourceExpr);
            }
        }

        private static IReadOnlyList<(string dtoPropName, Expression rhs)> GetBindings(Expression body)
        {
            if (body is MemberInitExpression mi)
            {
                var list = new List<(string dtoPropName, Expression rhs)>();
                foreach (var b in mi.Bindings)
                {
                    if (b is MemberAssignment ma)
                        list.Add((b.Member.Name, ma.Expression));
                }
                return list;
            }

            if (body is NewExpression ne && ne.Members != null)
            {
                var list = new List<(string dtoPropName, Expression rhs)>();
                for (var i = 0; i < ne.Arguments.Count; i++)
                {
                    list.Add((ne.Members[i].Name, ne.Arguments[i]));
                }
                return list;
            }

            throw new NotSupportedException("Projection must be a member-init (new TDto { ... }) or a new-expression with member mapping.");
        }
    }

    public sealed class JoinQueryViewBuilder<T1, T2, TView> where T1 : class where T2 : class where TView : class
    {
        private readonly JoinQueryBuilder<T1, T2> _inner;
        private readonly ViewMap<T1, T2, TView> _viewMap;

        private Expression<Func<TView, bool>>? _where;
        private readonly List<(Expression<Func<TView, object>> keySelector, bool desc)> _orderBys = new();

        internal JoinQueryViewBuilder(JoinQueryBuilder<T1, T2> inner, ViewMap<T1, T2, TView> viewMap)
        {
            _inner = inner;
            _viewMap = viewMap;
        }

        public JoinQueryViewBuilder<T1, T2, TView> Where(Expression<Func<TView, bool>> predicate)
        {
            _where = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, TView> OrderBy(Expression<Func<TView, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _orderBys.Clear();
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, TView> ThenBy(Expression<Func<TView, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (_orderBys.Count == 0)
                throw new InvalidOperationException("OrderBy must be called before ThenBy.");
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, TView> Page(int skip, int take)
        {
            _inner.Page(skip, take);
            return this;
        }

        public async Task<IReadOnlyList<TDto>> Select<TDto>(IDbConnection connection, IDbTransaction? transaction = null) where TDto : class
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var selectMap = SelectMap<T1, T2, TDto>.FromViewMap(_viewMap);
            var rewrittenWhere = _where == null ? null : ViewExpressionRewriter<T1, T2, TView>.RewritePredicate(_viewMap, _where);
            var rewrittenOrder = _orderBys.Count == 0
                ? null
                : _orderBys.Select(o => (ViewExpressionRewriter<T1, T2, TView>.RewriteKeySelector(_viewMap, o.keySelector), o.desc)).ToList();

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRenderer.Render(dialect, _inner, selectMap, rewrittenWhere, rewrittenOrder, tablePrefixProvider: null);

            var rows = await connection.QueryAsync<TDto>(sql, parameters, transaction);
            return rows.AsList();
        }
    }

    public sealed class JoinQuerySelectBuilder<T1, T2, TDto> where T1 : class where T2 : class where TDto : class
    {
        private readonly JoinQueryBuilder<T1, T2> _inner;
        private readonly SelectMap<T1, T2, TDto> _selectMap;

        internal JoinQuerySelectBuilder(JoinQueryBuilder<T1, T2> inner, SelectMap<T1, T2, TDto> selectMap)
        {
            _inner = inner;
            _selectMap = selectMap;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> Where(Expression<Func<T1, T2, bool>> predicate)
        {
            _inner.Where(predicate);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> OrderBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
        {
            _inner.OrderBy(keySelector, desc);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> ThenBy(Expression<Func<T1, T2, object>> keySelector, bool desc = false)
        {
            _inner.ThenBy(keySelector, desc);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> Page(int skip, int take)
        {
            _inner.Page(skip, take);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> Distinct(bool distinct = true)
        {
            _inner.SetDistinct(distinct);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> GroupBy(Expression<Func<T1, T2, object>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            _inner.SetGroupBy(keySelector);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, TDto> Having(Expression<Func<T1, T2, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _inner.SetHaving(predicate);
            return this;
        }

        public async Task<(string, DynamicParameters)> PrintToListSqlAsync(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRenderer.Render(dialect, _inner, _selectMap, whereExpr: null, orderByExprs: null, tablePrefixProvider: tablePrefixProvider);
            return (sql, parameters);
        }

        public async Task<IReadOnlyList<TDto>> ToListAsync(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRenderer.Render(dialect, _inner, _selectMap, whereExpr: null, orderByExprs: null, tablePrefixProvider: tablePrefixProvider);
            var rows = await connection.QueryAsync<TDto>(sql, parameters, transaction);
            return rows.AsList();
        }
    }

    public sealed class ViewMap<T1, T2, TView> where T1 : class where T2 : class where TView : class
    {
        private readonly string _a1;
        private readonly string _a2;

        private readonly Dictionary<string, LambdaExpression> _mappings = new(StringComparer.OrdinalIgnoreCase);
        private bool _frozen;

        internal ViewMap(string a1, string a2)
        {
            _a1 = a1;
            _a2 = a2;
        }

        public ViewMap<T1, T2, TView> Map(Expression<Func<TView, object>> viewProperty, Expression<Func<T1, T2, object>> source)
        {
            if (_frozen)
                throw new InvalidOperationException("ViewMap is frozen.");
            if (viewProperty == null)
                throw new ArgumentNullException(nameof(viewProperty));
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var name = ExpressionHelpers.GetPropertyName(viewProperty);
            _mappings[name] = source;
            return this;
        }

        internal void Freeze()
        {
            if (_mappings.Count == 0)
                throw new InvalidOperationException("At least one Map must be configured.");
            _frozen = true;
        }

        internal IReadOnlyDictionary<string, LambdaExpression> Mappings => _mappings;
        internal (string a1, string a2) Aliases => (_a1, _a2);
    }

    public sealed class SelectMap<T1, T2, TDto> where T1 : class where T2 : class where TDto : class
    {
        private readonly string _a1;
        private readonly string _a2;

        private readonly List<(string dtoProp, LambdaExpression source)> _columns = new();
        private bool _frozen;

        internal SelectMap(string a1, string a2)
        {
            _a1 = a1;
            _a2 = a2;
        }

        public SelectMap<T1, T2, TDto> Column(Expression<Func<TDto, object>> dtoProperty, Expression<Func<T1, T2, object>> source)
        {
            if (_frozen)
                throw new InvalidOperationException("SelectMap is frozen.");
            if (dtoProperty == null)
                throw new ArgumentNullException(nameof(dtoProperty));
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var name = ExpressionHelpers.GetPropertyName(dtoProperty);
            _columns.Add((name, source));
            return this;
        }

        internal void Freeze()
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("At least one Column must be configured.");
            _frozen = true;
        }

        internal IReadOnlyList<(string dtoProp, LambdaExpression source)> Columns => _columns;
        internal (string a1, string a2) Aliases => (_a1, _a2);

        internal static SelectMap<T1, T2, TDto> FromViewMap<TView>(ViewMap<T1, T2, TView> viewMap) where TView : class
        {
            var sm = new SelectMap<T1, T2, TDto>(viewMap.Aliases.a1, viewMap.Aliases.a2);

            var dtoProps = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in viewMap.Mappings)
            {
                if (!dtoProps.Contains(kv.Key))
                    continue;
                sm._columns.Add((kv.Key, kv.Value));
            }

            sm.Freeze();
            return sm;
        }
    }

    internal static class JoinSqlRenderer
    {
        public static (string sql, DynamicParameters parameters) Render<T1, T2, TDto>(
            IJoinQueryDialect dialect,
            JoinQueryBuilder<T1, T2> query,
            SelectMap<T1, T2, TDto> selectMap,
            Expression<Func<T1, T2, bool>>? whereExpr,
            IReadOnlyList<(Expression<Func<T1, T2, object>> keySelector, bool desc)>? orderByExprs,
            IDataTablePrefixProvider? tablePrefixProvider)
            where T1 : class where T2 : class where TDto : class
        {
            var (a1, a2) = query.Aliases;
            var onParams = query.On.Parameters;

            var paramAliases = new Dictionary<ParameterExpression, string>
            {
                [onParams[0]] = a1,
                [onParams[1]] = a2
            };

            var sqlBuilder = new JoinExpressionSqlBuilder(dialect, paramAliases);

            var sb = new StringBuilder();
            var parameters = new DynamicParameters();

            var t1Mapping = MappingUtils.GetOrAddMapping(typeof(T1));
            var t2Mapping = MappingUtils.GetOrAddMapping(typeof(T2));

            sb.Append("SELECT ");
            if (query.Distinct)
                sb.Append("DISTINCT ");
            sb.Append(string.Join(", ", selectMap.Columns.Select(col =>
            {
                var colSql = sqlBuilder.ToColumnSql(col.source, parameters);
                return $"{colSql} AS {dialect.QuoteIdentifier(col.dtoProp)}";
            })));

            sb.Append(" FROM ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithSchemaAndPrefix(t1Mapping.TableName, tablePrefixProvider))} {a1} ");

            sb.Append(query.JoinType == JoinType.Left ? "LEFT JOIN " : "JOIN ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithSchemaAndPrefix(t2Mapping.TableName, tablePrefixProvider))} {a2} ON ");
            sb.Append(sqlBuilder.ToPredicateSql(query.On.Body, parameters));

            if (query.WhereExpr != null)
            {
                sb.Append(" WHERE ");
                sb.Append(sqlBuilder.ToPredicateSql(query.WhereExpr.Body, parameters));
            }

            if (whereExpr != null)
            {
                sb.Append(query.WhereExpr == null ? " WHERE " : " AND ");
                sb.Append(sqlBuilder.ToPredicateSql(whereExpr.Body, parameters));
            }

            if (query.GroupByExpr != null)
            {
                sb.Append(" GROUP BY ");
                sb.Append(sqlBuilder.ToGroupBySql(query.GroupByExpr, parameters));
            }

            if (query.HavingExpr != null)
            {
                if (query.GroupByExpr == null)
                    throw new InvalidOperationException("HAVING requires GROUP BY.");

                sb.Append(" HAVING ");
                sb.Append(sqlBuilder.ToPredicateSql(query.HavingExpr.Body, parameters));
            }

            if (query.OrderBys.Count > 0)
            {
                sb.Append(" ORDER BY ");
                sb.Append(string.Join(", ", query.OrderBys.Select(o =>
                {
                    var key = sqlBuilder.ToOrderKeySql(o.keySelector, parameters);
                    return o.desc ? key + " DESC" : key + " ASC";
                })));
            }

            if (orderByExprs != null && orderByExprs.Count > 0)
            {
                if (query.OrderBys.Count > 0)
                    throw new InvalidOperationException("Cannot mix OrderBy on base query and view order-by.");

                sb.Append(" ORDER BY ");
                sb.Append(string.Join(", ", orderByExprs.Select(o =>
                {
                    var key = sqlBuilder.ToOrderKeySql(o.keySelector, parameters);
                    return o.desc ? key + " DESC" : key + " ASC";
                })));
            }

            if (query.PageSpec != null)
            {
                dialect.AppendPaging(sb, parameters, query.PageSpec.Value.skip, query.PageSpec.Value.take, hasOrderBy: query.OrderBys.Count > 0 || (orderByExprs != null && orderByExprs.Count > 0));
            }

            sb.Append(";");
            return (sb.ToString(), parameters);
        }
    }

    internal static class ViewExpressionRewriter<T1, T2, TView>
        where T1 : class where T2 : class where TView : class
    {
        public static Expression<Func<T1, T2, bool>> RewritePredicate(ViewMap<T1, T2, TView> viewMap, Expression<Func<TView, bool>> predicate)
        {
            if (viewMap == null)
                throw new ArgumentNullException(nameof(viewMap));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var p1 = Expression.Parameter(typeof(T1), "a");
            var p2 = Expression.Parameter(typeof(T2), "b");
            var visitor = new ViewMemberToSourceVisitor(viewMap.Mappings, predicate.Parameters[0], p1, p2);
            var body = visitor.Visit(predicate.Body) ?? predicate.Body;
            return Expression.Lambda<Func<T1, T2, bool>>(body, p1, p2);
        }

        public static Expression<Func<T1, T2, object>> RewriteKeySelector(ViewMap<T1, T2, TView> viewMap, Expression<Func<TView, object>> keySelector)
        {
            if (viewMap == null)
                throw new ArgumentNullException(nameof(viewMap));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var p1 = Expression.Parameter(typeof(T1), "a");
            var p2 = Expression.Parameter(typeof(T2), "b");
            var visitor = new ViewMemberToSourceVisitor(viewMap.Mappings, keySelector.Parameters[0], p1, p2);
            var body = visitor.Visit(keySelector.Body) ?? keySelector.Body;
            body = Expression.Convert(ExpressionHelpers.StripConvert(body), typeof(object));
            return Expression.Lambda<Func<T1, T2, object>>(body, p1, p2);
        }

        private sealed class ViewMemberToSourceVisitor : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<string, LambdaExpression> _viewMappings;
            private readonly ParameterExpression _viewParam;
            private readonly ParameterExpression _p1;
            private readonly ParameterExpression _p2;

            public ViewMemberToSourceVisitor(
                IReadOnlyDictionary<string, LambdaExpression> viewMappings,
                ParameterExpression viewParam,
                ParameterExpression p1,
                ParameterExpression p2)
            {
                _viewMappings = viewMappings;
                _viewParam = viewParam;
                _p1 = p1;
                _p2 = p2;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == _viewParam && node.Member is PropertyInfo pi)
                {
                    if (!_viewMappings.TryGetValue(pi.Name, out var mapped))
                        throw new InvalidOperationException($"View property '{pi.Name}' is not mapped.");

                    if (mapped.Parameters.Count != 2)
                        throw new InvalidOperationException($"Mapped source expression for '{pi.Name}' must have 2 parameters.");

                    var body = ExpressionHelpers.StripConvert(mapped.Body);
                    var rewritten = new SourceParameterRewriter(mapped.Parameters[0], mapped.Parameters[1], _p1, _p2).Visit(body);
                    return rewritten ?? node;
                }

                return base.VisitMember(node);
            }
        }

        private sealed class SourceParameterRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _s1;
            private readonly ParameterExpression _s2;
            private readonly ParameterExpression _t1;
            private readonly ParameterExpression _t2;

            public SourceParameterRewriter(ParameterExpression s1, ParameterExpression s2, ParameterExpression t1, ParameterExpression t2)
            {
                _s1 = s1;
                _s2 = s2;
                _t1 = t1;
                _t2 = t2;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _s1)
                    return _t1;
                if (node == _s2)
                    return _t2;
                return base.VisitParameter(node);
            }
        }
    }
}
