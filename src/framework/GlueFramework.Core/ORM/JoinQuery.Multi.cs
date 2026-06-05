using Dapper;
using GlueFramework.Core.Abstractions;
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
    public sealed class JoinQueryBuilder<T1, T2, T3> where T1 : class where T2 : class where T3 : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly string _a3;

        private readonly Expression<Func<T1, T2, bool>> _on12;
        private readonly JoinType _joinType2;

        private readonly Expression<Func<T1, T2, T3, bool>> _on3;
        private readonly JoinType _joinType3;

        private Expression<Func<T1, T2, T3, bool>>? _where;
        private readonly List<(LambdaExpression keySelector, bool desc)> _orderBys = new();
        private (int skip, int take)? _page;

        private bool _distinct;
        private LambdaExpression? _groupBy;
        private Expression<Func<T1, T2, T3, bool>>? _having;

        internal JoinQueryBuilder(
            string a1,
            string a2,
            Expression<Func<T1, T2, bool>> on12,
            JoinType joinType2,
            string a3,
            Expression<Func<T1, T2, T3, bool>> on3,
            JoinType joinType3)
        {
            _a1 = a1;
            _a2 = a2;
            _on12 = on12;
            _joinType2 = joinType2;

            _a3 = a3;
            _on3 = on3;
            _joinType3 = joinType3;
        }

        public JoinQueryBuilder<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate)
        {
            _where = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public JoinQueryBuilder<T1, T2, T3> OrderBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _orderBys.Clear();
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryBuilder<T1, T2, T3> ThenBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (_orderBys.Count == 0)
                throw new InvalidOperationException("OrderBy must be called before ThenBy.");
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryBuilder<T1, T2, T3> Page(int skip, int take)
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
        internal void SetHaving(Expression<Func<T1, T2, T3, bool>> predicate) => _having = predicate;

        public JoinQueryViewBuilder<T1, T2, T3, TView> UseView<TView>(Action<ViewMap<T1, T2, T3, TView>> map) where TView : class
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var vm = new ViewMap<T1, T2, T3, TView>(_a1, _a2, _a3);
            map(vm);
            vm.Freeze();
            return new JoinQueryViewBuilder<T1, T2, T3, TView>(this, vm);
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> Select<TDto>(Action<SelectMap<T1, T2, T3, TDto>> map) where TDto : class
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var sm = new SelectMap<T1, T2, T3, TDto>(_a1, _a2, _a3);
            map(sm);
            sm.Freeze();
            return new JoinQuerySelectBuilder<T1, T2, T3, TDto>(this, sm);
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> Select<TDto>(Expression<Func<T1, T2, T3, TDto>> projection) where TDto : class
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            var sm = new SelectMap<T1, T2, T3, TDto>(_a1, _a2, _a3);
            JoinQueryProjectionSelectMapBuilderMulti.Build(sm, projection);
            sm.Freeze();
            return new JoinQuerySelectBuilder<T1, T2, T3, TDto>(this, sm);
        }

        public JoinQueryBuilder<T1, T2, T3, T4> Join<T4>(
            string alias4,
            Expression<Func<T1, T2, T3, T4, bool>> on,
            JoinType joinType = JoinType.Inner) where T4 : class
        {
            if (string.IsNullOrWhiteSpace(alias4))
                throw new ArgumentException("Alias is required.", nameof(alias4));
            if (on == null)
                throw new ArgumentNullException(nameof(on));

            return new JoinQueryBuilder<T1, T2, T3, T4>(
                _a1,
                _a2,
                _on12,
                _joinType2,
                _a3,
                _on3,
                _joinType3,
                alias4,
                on,
                joinType);
        }

        public JoinQueryBuilder<T1, T2, T3, T4> Join<T4>(
            Expression<Func<T1, T2, T3, T4, bool>> on,
            JoinType joinType = JoinType.Inner) where T4 : class
        {
            if (on == null)
                throw new ArgumentNullException(nameof(on));

            return new JoinQueryBuilder<T1, T2, T3, T4>(
                _a1,
                _a2,
                _on12,
                _joinType2,
                _a3,
                _on3,
                _joinType3,
                "t4",
                on,
                joinType);
        }

        internal (string a1, string a2, string a3) Aliases => (_a1, _a2, _a3);
        internal (Expression<Func<T1, T2, bool>> on12, JoinType joinType2) Join2 => (_on12, _joinType2);
        internal (Expression<Func<T1, T2, T3, bool>> on3, JoinType joinType3) Join3 => (_on3, _joinType3);
        internal Expression<Func<T1, T2, T3, bool>>? WhereExpr => _where;
        internal IReadOnlyList<(LambdaExpression keySelector, bool desc)> OrderBys => _orderBys;
        internal (int skip, int take)? PageSpec => _page;

        internal bool Distinct => _distinct;
        internal LambdaExpression? GroupByExpr => _groupBy;
        internal Expression<Func<T1, T2, T3, bool>>? HavingExpr => _having;
    }

    internal static class JoinQueryProjectionSelectMapBuilderMulti
    {
        internal static void Build<T1, T2, T3, TDto>(SelectMap<T1, T2, T3, TDto> sm, Expression<Func<T1, T2, T3, TDto>> projection)
            where T1 : class where T2 : class where T3 : class where TDto : class
        {
            var bindings = GetBindings(projection.Body);
            foreach (var (dtoPropName, rhs) in bindings)
            {
                var dtoParam = Expression.Parameter(typeof(TDto), "dto");
                var dtoMember = Expression.PropertyOrField(dtoParam, dtoPropName);
                var dtoPropertyExpr = Expression.Lambda<Func<TDto, object>>(
                    Expression.Convert(dtoMember, typeof(object)),
                    dtoParam);

                var sourceExpr = Expression.Lambda<Func<T1, T2, T3, object>>(
                    Expression.Convert(rhs, typeof(object)),
                    projection.Parameters);

                sm.Column(dtoPropertyExpr, sourceExpr);
            }
        }

        internal static void Build<T1, T2, T3, T4, TDto>(SelectMap<T1, T2, T3, T4, TDto> sm, Expression<Func<T1, T2, T3, T4, TDto>> projection)
            where T1 : class where T2 : class where T3 : class where T4 : class where TDto : class
        {
            var bindings = GetBindings(projection.Body);
            foreach (var (dtoPropName, rhs) in bindings)
            {
                var dtoParam = Expression.Parameter(typeof(TDto), "dto");
                var dtoMember = Expression.PropertyOrField(dtoParam, dtoPropName);
                var dtoPropertyExpr = Expression.Lambda<Func<TDto, object>>(
                    Expression.Convert(dtoMember, typeof(object)),
                    dtoParam);

                var sourceExpr = Expression.Lambda<Func<T1, T2, T3, T4, object>>(
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

    public sealed class JoinQueryBuilder<T1, T2, T3, T4>
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly string _a3;
        private readonly string _a4;

        private readonly Expression<Func<T1, T2, bool>> _on12;
        private readonly JoinType _joinType2;

        private readonly Expression<Func<T1, T2, T3, bool>> _on3;
        private readonly JoinType _joinType3;

        private readonly Expression<Func<T1, T2, T3, T4, bool>> _on4;
        private readonly JoinType _joinType4;

        private Expression<Func<T1, T2, T3, T4, bool>>? _where;
        private readonly List<(LambdaExpression keySelector, bool desc)> _orderBys = new();
        private (int skip, int take)? _page;

        private bool _distinct;
        private LambdaExpression? _groupBy;
        private Expression<Func<T1, T2, T3, T4, bool>>? _having;

        internal JoinQueryBuilder(
            string a1,
            string a2,
            Expression<Func<T1, T2, bool>> on12,
            JoinType joinType2,
            string a3,
            Expression<Func<T1, T2, T3, bool>> on3,
            JoinType joinType3,
            string a4,
            Expression<Func<T1, T2, T3, T4, bool>> on4,
            JoinType joinType4)
        {
            _a1 = a1;
            _a2 = a2;
            _on12 = on12;
            _joinType2 = joinType2;

            _a3 = a3;
            _on3 = on3;
            _joinType3 = joinType3;

            _a4 = a4;
            _on4 = on4;
            _joinType4 = joinType4;
        }

        public JoinQueryBuilder<T1, T2, T3, T4> Where(Expression<Func<T1, T2, T3, T4, bool>> predicate)
        {
            _where = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public JoinQueryBuilder<T1, T2, T3, T4> OrderBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _orderBys.Clear();
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryBuilder<T1, T2, T3, T4> ThenBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (_orderBys.Count == 0)
                throw new InvalidOperationException("OrderBy must be called before ThenBy.");
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryBuilder<T1, T2, T3, T4> Page(int skip, int take)
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
        internal void SetHaving(Expression<Func<T1, T2, T3, T4, bool>> predicate) => _having = predicate;

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> UseView<TView>(Action<ViewMap<T1, T2, T3, T4, TView>> map) where TView : class
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var vm = new ViewMap<T1, T2, T3, T4, TView>(_a1, _a2, _a3, _a4);
            map(vm);
            vm.Freeze();
            return new JoinQueryViewBuilder<T1, T2, T3, T4, TView>(this, vm);
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> Select<TDto>(Action<SelectMap<T1, T2, T3, T4, TDto>> map) where TDto : class
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var sm = new SelectMap<T1, T2, T3, T4, TDto>(_a1, _a2, _a3, _a4);
            map(sm);
            sm.Freeze();
            return new JoinQuerySelectBuilder<T1, T2, T3, T4, TDto>(this, sm);
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> Select<TDto>(Expression<Func<T1, T2, T3, T4, TDto>> projection) where TDto : class
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            var sm = new SelectMap<T1, T2, T3, T4, TDto>(_a1, _a2, _a3, _a4);
            JoinQueryProjectionSelectMapBuilderMulti.Build(sm, projection);
            sm.Freeze();
            return new JoinQuerySelectBuilder<T1, T2, T3, T4, TDto>(this, sm);
        }

        internal (string a1, string a2, string a3, string a4) Aliases => (_a1, _a2, _a3, _a4);
        internal (Expression<Func<T1, T2, bool>> on12, JoinType joinType2) Join2 => (_on12, _joinType2);
        internal (Expression<Func<T1, T2, T3, bool>> on3, JoinType joinType3) Join3 => (_on3, _joinType3);
        internal (Expression<Func<T1, T2, T3, T4, bool>> on4, JoinType joinType4) Join4 => (_on4, _joinType4);
        internal Expression<Func<T1, T2, T3, T4, bool>>? WhereExpr => _where;
        internal IReadOnlyList<(LambdaExpression keySelector, bool desc)> OrderBys => _orderBys;
        internal (int skip, int take)? PageSpec => _page;

        internal bool Distinct => _distinct;
        internal LambdaExpression? GroupByExpr => _groupBy;
        internal Expression<Func<T1, T2, T3, T4, bool>>? HavingExpr => _having;
    }

    public sealed class ViewMap<T1, T2, T3, TView> where T1 : class where T2 : class where T3 : class where TView : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly string _a3;

        private readonly Dictionary<string, LambdaExpression> _mappings = new(StringComparer.OrdinalIgnoreCase);
        private bool _frozen;

        internal ViewMap(string a1, string a2, string a3)
        {
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
        }

        public ViewMap<T1, T2, T3, TView> Map(Expression<Func<TView, object>> viewProperty, Expression<Func<T1, T2, T3, object>> source)
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
        internal (string a1, string a2, string a3) Aliases => (_a1, _a2, _a3);
    }

    public sealed class ViewMap<T1, T2, T3, T4, TView>
        where T1 : class where T2 : class where T3 : class where T4 : class where TView : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly string _a3;
        private readonly string _a4;

        private readonly Dictionary<string, LambdaExpression> _mappings = new(StringComparer.OrdinalIgnoreCase);
        private bool _frozen;

        internal ViewMap(string a1, string a2, string a3, string a4)
        {
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
        }

        public ViewMap<T1, T2, T3, T4, TView> Map(Expression<Func<TView, object>> viewProperty, Expression<Func<T1, T2, T3, T4, object>> source)
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
        internal (string a1, string a2, string a3, string a4) Aliases => (_a1, _a2, _a3, _a4);
    }

    public sealed class SelectMap<T1, T2, T3, TDto> where T1 : class where T2 : class where T3 : class where TDto : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly string _a3;

        private readonly List<(string dtoProp, LambdaExpression source)> _columns = new();
        private bool _frozen;

        internal SelectMap(string a1, string a2, string a3)
        {
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
        }

        public SelectMap<T1, T2, T3, TDto> Column(Expression<Func<TDto, object>> dtoProperty, Expression<Func<T1, T2, T3, object>> source)
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
        internal (string a1, string a2, string a3) Aliases => (_a1, _a2, _a3);

        internal static SelectMap<T1, T2, T3, TDto> FromViewMap<TView>(ViewMap<T1, T2, T3, TView> viewMap) where TView : class
        {
            var sm = new SelectMap<T1, T2, T3, TDto>(viewMap.Aliases.a1, viewMap.Aliases.a2, viewMap.Aliases.a3);

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

    public sealed class SelectMap<T1, T2, T3, T4, TDto>
        where T1 : class where T2 : class where T3 : class where T4 : class where TDto : class
    {
        private readonly string _a1;
        private readonly string _a2;
        private readonly string _a3;
        private readonly string _a4;

        private readonly List<(string dtoProp, LambdaExpression source)> _columns = new();
        private bool _frozen;

        internal SelectMap(string a1, string a2, string a3, string a4)
        {
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
        }

        public SelectMap<T1, T2, T3, T4, TDto> Column(Expression<Func<TDto, object>> dtoProperty, Expression<Func<T1, T2, T3, T4, object>> source)
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
        internal (string a1, string a2, string a3, string a4) Aliases => (_a1, _a2, _a3, _a4);

        internal static SelectMap<T1, T2, T3, T4, TDto> FromViewMap<TView>(ViewMap<T1, T2, T3, T4, TView> viewMap) where TView : class
        {
            var sm = new SelectMap<T1, T2, T3, T4, TDto>(viewMap.Aliases.a1, viewMap.Aliases.a2, viewMap.Aliases.a3, viewMap.Aliases.a4);

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

    public sealed class JoinQuerySelectBuilder<T1, T2, T3, TDto>
        where T1 : class where T2 : class where T3 : class where TDto : class
    {
        private readonly JoinQueryBuilder<T1, T2, T3> _inner;
        private readonly SelectMap<T1, T2, T3, TDto> _selectMap;

        internal JoinQuerySelectBuilder(JoinQueryBuilder<T1, T2, T3> inner, SelectMap<T1, T2, T3, TDto> selectMap)
        {
            _inner = inner;
            _selectMap = selectMap;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> Where(Expression<Func<T1, T2, T3, bool>> predicate)
        {
            _inner.Where(predicate);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> OrderBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
        {
            _inner.OrderBy(keySelector, desc);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> ThenBy(Expression<Func<T1, T2, T3, object>> keySelector, bool desc = false)
        {
            _inner.ThenBy(keySelector, desc);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> Page(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take <= 0)
                throw new ArgumentOutOfRangeException(nameof(take));
            _inner.Page(skip, take);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> Distinct(bool distinct = true)
        {
            _inner.SetDistinct(distinct);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> GroupBy(Expression<Func<T1, T2, T3, object>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _inner.SetGroupBy(keySelector);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, TDto> Having(Expression<Func<T1, T2, T3, bool>> predicate)
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
            var (sql, parameters) = JoinSqlRendererMulti.Render(dialect, _inner, _selectMap, whereExpr: null, orderByExprs: null, tablePrefixProvider: tablePrefixProvider);
            return (sql, parameters);
        }

        public async Task<IReadOnlyList<TDto>> ToListAsync(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRendererMulti.Render(dialect, _inner, _selectMap, whereExpr: null, orderByExprs: null, tablePrefixProvider: tablePrefixProvider);
            var rows = await connection.QueryAsync<TDto>(sql, parameters, transaction);
            return rows.AsList();
        }
    }

    public sealed class JoinQueryViewBuilder<T1, T2, T3, TView>
        where T1 : class where T2 : class where T3 : class where TView : class
    {
        private readonly JoinQueryBuilder<T1, T2, T3> _inner;
        private readonly ViewMap<T1, T2, T3, TView> _viewMap;

        private Expression<Func<TView, bool>>? _where;
        private readonly List<(Expression<Func<TView, object>> keySelector, bool desc)> _orderBys = new();

        internal JoinQueryViewBuilder(JoinQueryBuilder<T1, T2, T3> inner, ViewMap<T1, T2, T3, TView> viewMap)
        {
            _inner = inner;
            _viewMap = viewMap;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> Where(Expression<Func<TView, bool>> predicate)
        {
            _where = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> OrderBy(Expression<Func<TView, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _orderBys.Clear();
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> ThenBy(Expression<Func<TView, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (_orderBys.Count == 0)
                throw new InvalidOperationException("OrderBy must be called before ThenBy.");
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> Page(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take <= 0)
                throw new ArgumentOutOfRangeException(nameof(take));
            _inner.Page(skip, take);
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> Distinct(bool distinct = true)
        {
            _inner.SetDistinct(distinct);
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> GroupBy(Expression<Func<T1, T2, T3, object>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _inner.SetGroupBy(keySelector);
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, TView> Having(Expression<Func<T1, T2, T3, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            _inner.SetHaving(predicate);
            return this;
        }

        public async Task<IReadOnlyList<TDto>> Select<TDto>(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null) where TDto : class
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var selectMap = SelectMap<T1, T2, T3, TDto>.FromViewMap(_viewMap);
            var rewrittenWhere = _where == null ? null : ViewExpressionRewriter<T1, T2, T3, TView>.RewritePredicate(_viewMap, _where);
            var rewrittenOrder = _orderBys.Count == 0
                ? null
                : _orderBys.Select(o => (ViewExpressionRewriter<T1, T2, T3, TView>.RewriteKeySelector(_viewMap, o.keySelector), o.desc)).ToList();

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRendererMulti.Render(dialect, _inner, selectMap, rewrittenWhere, rewrittenOrder, tablePrefixProvider: tablePrefixProvider);
            var rows = await connection.QueryAsync<TDto>(sql, parameters, transaction);
            return rows.AsList();
        }
    }

    public sealed class JoinQueryViewBuilder<T1, T2, T3, T4, TView>
        where T1 : class where T2 : class where T3 : class where T4 : class where TView : class
    {
        private readonly JoinQueryBuilder<T1, T2, T3, T4> _inner;
        private readonly ViewMap<T1, T2, T3, T4, TView> _viewMap;

        private Expression<Func<TView, bool>>? _where;
        private readonly List<(Expression<Func<TView, object>> keySelector, bool desc)> _orderBys = new();

        internal JoinQueryViewBuilder(JoinQueryBuilder<T1, T2, T3, T4> inner, ViewMap<T1, T2, T3, T4, TView> viewMap)
        {
            _inner = inner;
            _viewMap = viewMap;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> Where(Expression<Func<TView, bool>> predicate)
        {
            _where = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> OrderBy(Expression<Func<TView, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _orderBys.Clear();
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> ThenBy(Expression<Func<TView, object>> keySelector, bool desc = false)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (_orderBys.Count == 0)
                throw new InvalidOperationException("OrderBy must be called before ThenBy.");
            _orderBys.Add((keySelector, desc));
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> Page(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take <= 0)
                throw new ArgumentOutOfRangeException(nameof(take));
            _inner.Page(skip, take);
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> Distinct(bool distinct = true)
        {
            _inner.SetDistinct(distinct);
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> GroupBy(Expression<Func<T1, T2, T3, T4, object>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _inner.SetGroupBy(keySelector);
            return this;
        }

        public JoinQueryViewBuilder<T1, T2, T3, T4, TView> Having(Expression<Func<T1, T2, T3, T4, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            _inner.SetHaving(predicate);
            return this;
        }

        public async Task<IReadOnlyList<TDto>> Select<TDto>(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null) where TDto : class
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var selectMap = SelectMap<T1, T2, T3, T4, TDto>.FromViewMap(_viewMap);
            var rewrittenWhere = _where == null ? null : ViewExpressionRewriter<T1, T2, T3, T4, TView>.RewritePredicate(_viewMap, _where);
            var rewrittenOrder = _orderBys.Count == 0
                ? null
                : _orderBys.Select(o => (ViewExpressionRewriter<T1, T2, T3, T4, TView>.RewriteKeySelector(_viewMap, o.keySelector), o.desc)).ToList();

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRendererMulti.Render(dialect, _inner, selectMap, rewrittenWhere, rewrittenOrder, tablePrefixProvider: tablePrefixProvider);
            var rows = await connection.QueryAsync<TDto>(sql, parameters, transaction);
            return rows.AsList();
        }
    }

    public sealed class JoinQuerySelectBuilder<T1, T2, T3, T4, TDto>
        where T1 : class where T2 : class where T3 : class where T4 : class where TDto : class
    {
        private readonly JoinQueryBuilder<T1, T2, T3, T4> _inner;
        private readonly SelectMap<T1, T2, T3, T4, TDto> _selectMap;

        internal JoinQuerySelectBuilder(JoinQueryBuilder<T1, T2, T3, T4> inner, SelectMap<T1, T2, T3, T4, TDto> selectMap)
        {
            _inner = inner;
            _selectMap = selectMap;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> Where(Expression<Func<T1, T2, T3, T4, bool>> predicate)
        {
            _inner.Where(predicate);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> OrderBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
        {
            _inner.OrderBy(keySelector, desc);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> ThenBy(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc = false)
        {
            _inner.ThenBy(keySelector, desc);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> Page(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take <= 0)
                throw new ArgumentOutOfRangeException(nameof(take));
            _inner.Page(skip, take);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> Distinct(bool distinct = true)
        {
            _inner.SetDistinct(distinct);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> GroupBy(Expression<Func<T1, T2, T3, T4, object>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            _inner.SetGroupBy(keySelector);
            return this;
        }

        public JoinQuerySelectBuilder<T1, T2, T3, T4, TDto> Having(Expression<Func<T1, T2, T3, T4, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            _inner.SetHaving(predicate);
            return this;
        }

        public async Task<IReadOnlyList<TDto>> ToListAsync(IDbConnection connection, IDbTransaction? transaction = null, IDataTablePrefixProvider? tablePrefixProvider = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var dialect = JoinQueryDialectFactory.FromConnection(connection);
            var (sql, parameters) = JoinSqlRendererMulti.Render(dialect, _inner, _selectMap, whereExpr: null, orderByExprs: null, tablePrefixProvider: tablePrefixProvider);
            var rows = await connection.QueryAsync<TDto>(sql, parameters, transaction);
            return rows.AsList();
        }
    }

    internal static class JoinSqlRendererMulti
    {
        public static (string sql, DynamicParameters parameters) Render<T1, T2, T3, TDto>(
            IJoinQueryDialect dialect,
            JoinQueryBuilder<T1, T2, T3> query,
            SelectMap<T1, T2, T3, TDto> selectMap,
            Expression<Func<T1, T2, T3, bool>>? whereExpr,
            IReadOnlyList<(Expression<Func<T1, T2, T3, object>> keySelector, bool desc)>? orderByExprs,
            IDataTablePrefixProvider? tablePrefixProvider)
            where T1 : class where T2 : class where T3 : class where TDto : class
        {
            var (a1, a2, a3) = query.Aliases;

            var aliasMap = new Dictionary<ParameterExpression, string>();

            aliasMap[query.Join2.on12.Parameters[0]] = a1;
            aliasMap[query.Join2.on12.Parameters[1]] = a2;

            aliasMap[query.Join3.on3.Parameters[0]] = a1;
            aliasMap[query.Join3.on3.Parameters[1]] = a2;
            aliasMap[query.Join3.on3.Parameters[2]] = a3;

            var sqlBuilder = new JoinExpressionSqlBuilder(dialect, aliasMap);
            var sb = new StringBuilder();
            var parameters = new DynamicParameters();

            var t1Mapping = MappingUtils.GetOrAddMapping(typeof(T1));
            var t2Mapping = MappingUtils.GetOrAddMapping(typeof(T2));
            var t3Mapping = MappingUtils.GetOrAddMapping(typeof(T3));

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

            sb.Append(query.Join2.joinType2 == JoinType.Left ? "LEFT JOIN " : "JOIN ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithSchemaAndPrefix(t2Mapping.TableName, tablePrefixProvider))} {a2} ON ");
            sb.Append(sqlBuilder.ToPredicateSql(query.Join2.on12.Body, parameters));

            sb.Append(" ");
            sb.Append(query.Join3.joinType3 == JoinType.Left ? "LEFT JOIN " : "JOIN ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithSchemaAndPrefix(t3Mapping.TableName, tablePrefixProvider))} {a3} ON ");
            sb.Append(sqlBuilder.ToPredicateSql(query.Join3.on3.Body, parameters));

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

        public static (string sql, DynamicParameters parameters) Render<T1, T2, T3, T4, TDto>(
            IJoinQueryDialect dialect,
            JoinQueryBuilder<T1, T2, T3, T4> query,
            SelectMap<T1, T2, T3, T4, TDto> selectMap,
            Expression<Func<T1, T2, T3, T4, bool>>? whereExpr,
            IReadOnlyList<(Expression<Func<T1, T2, T3, T4, object>> keySelector, bool desc)>? orderByExprs,
            IDataTablePrefixProvider? tablePrefixProvider)
            where T1 : class where T2 : class where T3 : class where T4 : class where TDto : class
        {
            var (a1, a2, a3, a4) = query.Aliases;

            var aliasMap = new Dictionary<ParameterExpression, string>();

            aliasMap[query.Join2.on12.Parameters[0]] = a1;
            aliasMap[query.Join2.on12.Parameters[1]] = a2;

            aliasMap[query.Join3.on3.Parameters[0]] = a1;
            aliasMap[query.Join3.on3.Parameters[1]] = a2;
            aliasMap[query.Join3.on3.Parameters[2]] = a3;

            aliasMap[query.Join4.on4.Parameters[0]] = a1;
            aliasMap[query.Join4.on4.Parameters[1]] = a2;
            aliasMap[query.Join4.on4.Parameters[2]] = a3;
            aliasMap[query.Join4.on4.Parameters[3]] = a4;

            var sqlBuilder = new JoinExpressionSqlBuilder(dialect, aliasMap);
            var sb = new StringBuilder();
            var parameters = new DynamicParameters();

            var t1Mapping = MappingUtils.GetOrAddMapping(typeof(T1));
            var t2Mapping = MappingUtils.GetOrAddMapping(typeof(T2));
            var t3Mapping = MappingUtils.GetOrAddMapping(typeof(T3));
            var t4Mapping = MappingUtils.GetOrAddMapping(typeof(T4));

            sb.Append("SELECT ");
            if (query.Distinct)
                sb.Append("DISTINCT ");
            sb.Append(string.Join(", ", selectMap.Columns.Select(col =>
            {
                var colSql = sqlBuilder.ToColumnSql(col.source, parameters);
                return $"{colSql} AS {dialect.QuoteIdentifier(col.dtoProp)}";
            })));

            sb.Append(" FROM ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithPrefix(t1Mapping.TableName, tablePrefixProvider))} {a1} ");

            sb.Append(query.Join2.joinType2 == JoinType.Left ? "LEFT JOIN " : "JOIN ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithPrefix(t2Mapping.TableName, tablePrefixProvider))} {a2} ON ");
            sb.Append(sqlBuilder.ToPredicateSql(query.Join2.on12.Body, parameters));

            sb.Append(" ");
            sb.Append(query.Join3.joinType3 == JoinType.Left ? "LEFT JOIN " : "JOIN ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithPrefix(t3Mapping.TableName, tablePrefixProvider))} {a3} ON ");
            sb.Append(sqlBuilder.ToPredicateSql(query.Join3.on3.Body, parameters));

            sb.Append(" ");
            sb.Append(query.Join4.joinType4 == JoinType.Left ? "LEFT JOIN " : "JOIN ");
            sb.Append($"{dialect.QuoteIdentifier(JoinQueryTableName.WithPrefix(t4Mapping.TableName, tablePrefixProvider))} {a4} ON ");
            sb.Append(sqlBuilder.ToPredicateSql(query.Join4.on4.Body, parameters));

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

    internal static class ViewExpressionRewriter<T1, T2, T3, TView>
        where T1 : class where T2 : class where T3 : class where TView : class
    {
        public static Expression<Func<T1, T2, T3, bool>> RewritePredicate(ViewMap<T1, T2, T3, TView> viewMap, Expression<Func<TView, bool>> predicate)
        {
            if (viewMap == null)
                throw new ArgumentNullException(nameof(viewMap));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var p1 = Expression.Parameter(typeof(T1), "a");
            var p2 = Expression.Parameter(typeof(T2), "b");
            var p3 = Expression.Parameter(typeof(T3), "c");
            var visitor = new ViewMemberToSourceVisitor(viewMap.Mappings, predicate.Parameters[0], p1, p2, p3);
            var body = visitor.Visit(predicate.Body) ?? predicate.Body;
            return Expression.Lambda<Func<T1, T2, T3, bool>>(body, p1, p2, p3);
        }

        public static Expression<Func<T1, T2, T3, object>> RewriteKeySelector(ViewMap<T1, T2, T3, TView> viewMap, Expression<Func<TView, object>> keySelector)
        {
            if (viewMap == null)
                throw new ArgumentNullException(nameof(viewMap));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var p1 = Expression.Parameter(typeof(T1), "a");
            var p2 = Expression.Parameter(typeof(T2), "b");
            var p3 = Expression.Parameter(typeof(T3), "c");
            var visitor = new ViewMemberToSourceVisitor(viewMap.Mappings, keySelector.Parameters[0], p1, p2, p3);
            var body = visitor.Visit(keySelector.Body) ?? keySelector.Body;
            body = Expression.Convert(ExpressionHelpers.StripConvert(body), typeof(object));
            return Expression.Lambda<Func<T1, T2, T3, object>>(body, p1, p2, p3);
        }

        private sealed class ViewMemberToSourceVisitor : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<string, LambdaExpression> _viewMappings;
            private readonly ParameterExpression _viewParam;
            private readonly ParameterExpression _p1;
            private readonly ParameterExpression _p2;
            private readonly ParameterExpression _p3;

            public ViewMemberToSourceVisitor(
                IReadOnlyDictionary<string, LambdaExpression> viewMappings,
                ParameterExpression viewParam,
                ParameterExpression p1,
                ParameterExpression p2,
                ParameterExpression p3)
            {
                _viewMappings = viewMappings;
                _viewParam = viewParam;
                _p1 = p1;
                _p2 = p2;
                _p3 = p3;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == _viewParam && node.Member is PropertyInfo pi)
                {
                    if (!_viewMappings.TryGetValue(pi.Name, out var mapped))
                        throw new InvalidOperationException($"View property '{pi.Name}' is not mapped.");

                    if (mapped.Parameters.Count != 3)
                        throw new InvalidOperationException($"Mapped source expression for '{pi.Name}' must have 3 parameters.");

                    var body = ExpressionHelpers.StripConvert(mapped.Body);
                    var rewritten = new SourceParameterRewriter(mapped.Parameters[0], mapped.Parameters[1], mapped.Parameters[2], _p1, _p2, _p3)
                        .Visit(body);
                    return rewritten ?? node;
                }

                return base.VisitMember(node);
            }
        }

        private sealed class SourceParameterRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _s1;
            private readonly ParameterExpression _s2;
            private readonly ParameterExpression _s3;
            private readonly ParameterExpression _t1;
            private readonly ParameterExpression _t2;
            private readonly ParameterExpression _t3;

            public SourceParameterRewriter(ParameterExpression s1, ParameterExpression s2, ParameterExpression s3, ParameterExpression t1, ParameterExpression t2, ParameterExpression t3)
            {
                _s1 = s1;
                _s2 = s2;
                _s3 = s3;
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _s1)
                    return _t1;
                if (node == _s2)
                    return _t2;
                if (node == _s3)
                    return _t3;
                return base.VisitParameter(node);
            }
        }
    }

    internal static class ViewExpressionRewriter<T1, T2, T3, T4, TView>
        where T1 : class where T2 : class where T3 : class where T4 : class where TView : class
    {
        public static Expression<Func<T1, T2, T3, T4, bool>> RewritePredicate(ViewMap<T1, T2, T3, T4, TView> viewMap, Expression<Func<TView, bool>> predicate)
        {
            if (viewMap == null)
                throw new ArgumentNullException(nameof(viewMap));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var p1 = Expression.Parameter(typeof(T1), "a");
            var p2 = Expression.Parameter(typeof(T2), "b");
            var p3 = Expression.Parameter(typeof(T3), "c");
            var p4 = Expression.Parameter(typeof(T4), "d");
            var visitor = new ViewMemberToSourceVisitor(viewMap.Mappings, predicate.Parameters[0], p1, p2, p3, p4);
            var body = visitor.Visit(predicate.Body) ?? predicate.Body;
            return Expression.Lambda<Func<T1, T2, T3, T4, bool>>(body, p1, p2, p3, p4);
        }

        public static Expression<Func<T1, T2, T3, T4, object>> RewriteKeySelector(ViewMap<T1, T2, T3, T4, TView> viewMap, Expression<Func<TView, object>> keySelector)
        {
            if (viewMap == null)
                throw new ArgumentNullException(nameof(viewMap));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var p1 = Expression.Parameter(typeof(T1), "a");
            var p2 = Expression.Parameter(typeof(T2), "b");
            var p3 = Expression.Parameter(typeof(T3), "c");
            var p4 = Expression.Parameter(typeof(T4), "d");
            var visitor = new ViewMemberToSourceVisitor(viewMap.Mappings, keySelector.Parameters[0], p1, p2, p3, p4);
            var body = visitor.Visit(keySelector.Body) ?? keySelector.Body;
            body = Expression.Convert(ExpressionHelpers.StripConvert(body), typeof(object));
            return Expression.Lambda<Func<T1, T2, T3, T4, object>>(body, p1, p2, p3, p4);
        }

        private sealed class ViewMemberToSourceVisitor : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<string, LambdaExpression> _viewMappings;
            private readonly ParameterExpression _viewParam;
            private readonly ParameterExpression _p1;
            private readonly ParameterExpression _p2;
            private readonly ParameterExpression _p3;
            private readonly ParameterExpression _p4;

            public ViewMemberToSourceVisitor(
                IReadOnlyDictionary<string, LambdaExpression> viewMappings,
                ParameterExpression viewParam,
                ParameterExpression p1,
                ParameterExpression p2,
                ParameterExpression p3,
                ParameterExpression p4)
            {
                _viewMappings = viewMappings;
                _viewParam = viewParam;
                _p1 = p1;
                _p2 = p2;
                _p3 = p3;
                _p4 = p4;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == _viewParam && node.Member is PropertyInfo pi)
                {
                    if (!_viewMappings.TryGetValue(pi.Name, out var mapped))
                        throw new InvalidOperationException($"View property '{pi.Name}' is not mapped.");

                    if (mapped.Parameters.Count != 4)
                        throw new InvalidOperationException($"Mapped source expression for '{pi.Name}' must have 4 parameters.");

                    var body = ExpressionHelpers.StripConvert(mapped.Body);
                    var rewritten = new SourceParameterRewriter(mapped.Parameters[0], mapped.Parameters[1], mapped.Parameters[2], mapped.Parameters[3], _p1, _p2, _p3, _p4)
                        .Visit(body);
                    return rewritten ?? node;
                }

                return base.VisitMember(node);
            }
        }

        private sealed class SourceParameterRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _s1;
            private readonly ParameterExpression _s2;
            private readonly ParameterExpression _s3;
            private readonly ParameterExpression _s4;
            private readonly ParameterExpression _t1;
            private readonly ParameterExpression _t2;
            private readonly ParameterExpression _t3;
            private readonly ParameterExpression _t4;

            public SourceParameterRewriter(
                ParameterExpression s1,
                ParameterExpression s2,
                ParameterExpression s3,
                ParameterExpression s4,
                ParameterExpression t1,
                ParameterExpression t2,
                ParameterExpression t3,
                ParameterExpression t4)
            {
                _s1 = s1;
                _s2 = s2;
                _s3 = s3;
                _s4 = s4;
                _t1 = t1;
                _t2 = t2;
                _t3 = t3;
                _t4 = t4;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _s1)
                    return _t1;
                if (node == _s2)
                    return _t2;
                if (node == _s3)
                    return _t3;
                if (node == _s4)
                    return _t4;
                return base.VisitParameter(node);
            }
        }
    }
}
