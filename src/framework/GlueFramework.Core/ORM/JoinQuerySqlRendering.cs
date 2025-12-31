using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GlueFramework.Core.ORM
{
    internal sealed class JoinExpressionSqlBuilder
    {
        private readonly IJoinQueryDialect _dialect;
        private readonly IReadOnlyDictionary<ParameterExpression, string> _aliases;
        private readonly IReadOnlyDictionary<string, string> _aliasesByName;
        private int _pIndex = 1;

        private const int MaxInListSize = 1000;

        public JoinExpressionSqlBuilder(IJoinQueryDialect dialect, IReadOnlyDictionary<ParameterExpression, string> aliases)
        {
            _dialect = dialect;
            _aliases = aliases;
            _aliasesByName = aliases
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key.Name))
                .GroupBy(kv => kv.Key.Name!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        }

        public string ToPredicateSql(Expression expression, DynamicParameters parameters)
        {
            return Visit(expression, parameters, context: VisitContext.Predicate);
        }

        public string ToOrderKeySql(LambdaExpression keySelector, DynamicParameters parameters)
        {
            var body = ExpressionHelpers.StripConvert(keySelector.Body);
            return Visit(body, parameters, context: VisitContext.Column);
        }

        public string ToColumnSql(LambdaExpression source, DynamicParameters parameters)
        {
            var body = ExpressionHelpers.StripConvert(source.Body);
            return Visit(body, parameters, context: VisitContext.Column);
        }

        public string ToGroupBySql(LambdaExpression keySelector, DynamicParameters parameters)
        {
            var body = ExpressionHelpers.StripConvert(keySelector.Body);

            if (body is NewExpression ne)
            {
                return string.Join(", ", ne.Arguments.Select(a => Visit(a, parameters, VisitContext.Column)));
            }

            if (body is MemberInitExpression mi)
            {
                var parts = new List<string>();
                foreach (var b in mi.Bindings)
                {
                    if (b is MemberAssignment ma)
                        parts.Add(Visit(ma.Expression, parameters, VisitContext.Column));
                }
                return string.Join(", ", parts);
            }

            return Visit(body, parameters, VisitContext.Column);
        }

        private string Visit(Expression expr, DynamicParameters parameters, VisitContext context)
        {
            expr = ExpressionHelpers.StripConvert(expr);

            if (expr is BinaryExpression be)
                return VisitBinary(be, parameters, context);
            if (expr is MemberExpression me)
                return VisitMember(me, parameters, context);
            if (expr is ConstantExpression ce)
                return VisitConstant(ce, parameters);
            if (expr is MethodCallExpression mce)
                return VisitMethodCall(mce, parameters);
            if (expr is UnaryExpression ue)
                return VisitUnary(ue, parameters, context);

            throw new NotSupportedException($"Unsupported expression: {expr.GetType().Name}");
        }

        private string VisitUnary(UnaryExpression ue, DynamicParameters parameters, VisitContext context)
        {
            if (ue.NodeType == ExpressionType.Not)
            {
                if (ue.Operand is MemberExpression || ue.Operand is ConstantExpression)
                {
                    var value = ExpressionHelpers.GetValue(ue.Operand);
                    var param = AddParam(parameters, value);

                    return _dialect.NotBoolean(param);
                }

                return $"NOT ({Visit(ue.Operand, parameters, VisitContext.Predicate)})";
            }

            return Visit(ue.Operand, parameters, context);
        }

        private string VisitBinary(BinaryExpression be, DynamicParameters parameters, VisitContext context)
        {
            var left = ExpressionHelpers.StripConvert(be.Left);
            var right = ExpressionHelpers.StripConvert(be.Right);

            if (be.NodeType == ExpressionType.Add || be.NodeType == ExpressionType.Subtract ||
                be.NodeType == ExpressionType.Multiply || be.NodeType == ExpressionType.Divide ||
                be.NodeType == ExpressionType.Modulo)
            {
                var op = be.NodeType switch
                {
                    ExpressionType.Add => "+",
                    ExpressionType.Subtract => "-",
                    ExpressionType.Multiply => "*",
                    ExpressionType.Divide => "/",
                    ExpressionType.Modulo => "%",
                    _ => throw new NotSupportedException($"Unsupported arithmetic node: {be.NodeType}")
                };

                var l = Visit(left, parameters, VisitContext.Column);
                var r = Visit(right, parameters, VisitContext.Column);
                return $"({l} {op} {r})";
            }

            if (be.NodeType == ExpressionType.AndAlso || be.NodeType == ExpressionType.OrElse)
            {
                var op = be.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({Visit(left, parameters, VisitContext.Predicate)} {op} {Visit(right, parameters, VisitContext.Predicate)})";
            }

            if (be.NodeType == ExpressionType.Equal || be.NodeType == ExpressionType.NotEqual)
            {
                var isNullRight = ExpressionHelpers.IsNullConstant(right);
                var isNullLeft = ExpressionHelpers.IsNullConstant(left);
                if (isNullRight || isNullLeft)
                {
                    var colExpr = isNullRight ? left : right;
                    var colSql = Visit(colExpr, parameters, VisitContext.Column);
                    return be.NodeType == ExpressionType.Equal ? $"({colSql} IS NULL)" : $"({colSql} IS NOT NULL)";
                }
            }

            var op2 = be.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Unsupported binary node: {be.NodeType}")
            };

            var leftSql = Visit(left, parameters, VisitContext.Column);
            var rightSql = IsSqlScalarExpression(right)
                ? Visit(right, parameters, VisitContext.Column)
                : Visit(right, parameters, VisitContext.Predicate);
            return $"({leftSql} {op2} {rightSql})";
        }

        private string VisitMember(MemberExpression me, DynamicParameters parameters, VisitContext context)
        {
            if (me.Member is PropertyInfo pi)
            {
                if (me.Expression is ParameterExpression pe && _aliases.TryGetValue(pe, out var alias))
                {
                    var col = MappingUtils.GetColumnName(pi);
                    return $"{alias}.{_dialect.QuoteIdentifier(col)}";
                }

                if (me.Expression is ParameterExpression pe2 && !string.IsNullOrWhiteSpace(pe2.Name)
                    && _aliasesByName.TryGetValue(pe2.Name!, out var alias2))
                {
                    var col = MappingUtils.GetColumnName(pi);
                    return $"{alias2}.{_dialect.QuoteIdentifier(col)}";
                }

                if (context == VisitContext.Column)
                    throw new NotSupportedException($"Only direct column access is supported in this context: {me}");

                var value = Expression.Lambda(Expression.Convert(me, typeof(object))).Compile().DynamicInvoke();
                return AddParam(parameters, value);
            }

            if (context == VisitContext.Column)
                throw new NotSupportedException($"Only direct column access is supported in this context: {me}");

            var value2 = Expression.Lambda(Expression.Convert(me, typeof(object))).Compile().DynamicInvoke();
            return AddParam(parameters, value2);
        }

        private string VisitConstant(ConstantExpression ce, DynamicParameters parameters)
        {
            return AddParam(parameters, ce.Value);
        }

        private string VisitMethodCall(MethodCallExpression mce, DynamicParameters parameters)
        {
            if (mce.Method.DeclaringType == typeof(SqlFn))
            {
                var name = mce.Method.Name;

                if (name == nameof(SqlFn.Distinct) && mce.Arguments.Count == 1)
                {
                    var inner = Visit(mce.Arguments[0], parameters, VisitContext.Column);
                    return $"DISTINCT {inner}";
                }

                if (name == nameof(SqlFn.Count) && mce.Arguments.Count == 0)
                {
                    return "COUNT(1)";
                }

                if (name == nameof(SqlFn.Count) && mce.Arguments.Count == 1)
                {
                    var inner = Visit(mce.Arguments[0], parameters, VisitContext.Column);
                    return $"COUNT({inner})";
                }

                if ((name == nameof(SqlFn.Sum) || name == nameof(SqlFn.Avg) || name == nameof(SqlFn.Min) || name == nameof(SqlFn.Max))
                    && mce.Arguments.Count == 1)
                {
                    var func = name.ToUpperInvariant();
                    var inner = Visit(mce.Arguments[0], parameters, VisitContext.Column);
                    return $"{func}({inner})";
                }

                throw new NotSupportedException($"Unsupported SqlFn method: {name}");
            }

            if (mce.Method.Name == nameof(Enumerable.Contains))
            {
                if (TryBuildInClause(mce, parameters, out var inSql))
                    return inSql;
            }

            if (mce.Method.DeclaringType == typeof(string))
            {
                if (mce.Method.Name == nameof(string.Contains) && mce.Arguments.Count == 1)
                {
                    var left = Visit(mce.Object!, parameters, VisitContext.Column);
                    var argValue = ExpressionHelpers.GetValue(mce.Arguments[0]);
                    return $"({left} {_dialect.LikeOperator} {AddParam(parameters, $"%{argValue}%")})";
                }

                if (mce.Method.Name == nameof(string.StartsWith) && mce.Arguments.Count == 1)
                {
                    var left = Visit(mce.Object!, parameters, VisitContext.Column);
                    var argValue = ExpressionHelpers.GetValue(mce.Arguments[0]);
                    return $"({left} {_dialect.LikeOperator} {AddParam(parameters, $"{argValue}%")})";
                }

                if (mce.Method.Name == nameof(string.EndsWith) && mce.Arguments.Count == 1)
                {
                    var left = Visit(mce.Object!, parameters, VisitContext.Column);
                    var argValue = ExpressionHelpers.GetValue(mce.Arguments[0]);
                    return $"({left} {_dialect.LikeOperator} {AddParam(parameters, $"%{argValue}")})";
                }

                if (mce.Method.Name == nameof(string.IsNullOrWhiteSpace)
                    && mce.Arguments.Count == 1)
                {
                    var arg = mce.Arguments[0];

                    if (IsColumnAccess(arg))
                    {
                        var col = Visit(arg, parameters, VisitContext.Column);
                        return $"({col} IS NULL OR LTRIM(RTRIM({col})) = '')";
                    }

                    var value = ExpressionHelpers.GetValue(arg);
                    var param = AddParam(parameters, value);
                    return $"({param} IS NULL OR LTRIM(RTRIM({param})) = '')";
                }
            }

            throw new NotSupportedException($"Unsupported method call: {mce.Method.DeclaringType?.Name}.{mce.Method.Name}");
        }

        private bool TryBuildInClause(MethodCallExpression mce, DynamicParameters parameters, out string sql)
        {
            sql = string.Empty;

            if (mce.Method.DeclaringType == typeof(string))
                return false;

            Expression? listExpr = null;
            Expression? valueExpr = null;

            if (mce.Object != null && mce.Arguments.Count == 1 && mce.Method.Name == nameof(Enumerable.Contains))
            {
                listExpr = mce.Object;
                valueExpr = mce.Arguments[0];
            }

            if (mce.Object == null
                && mce.Arguments.Count == 2
                && mce.Method.DeclaringType == typeof(Enumerable)
                && mce.Method.Name == nameof(Enumerable.Contains))
            {
                listExpr = mce.Arguments[0];
                valueExpr = mce.Arguments[1];
            }

            if (listExpr == null || valueExpr == null)
                return false;

            if (!IsColumnAccess(valueExpr))
                return false;

            var listValueObj = ExpressionHelpers.GetValue(listExpr);
            if (listValueObj == null)
                throw new InvalidOperationException("IN list is null.");

            if (listValueObj is string)
                return false;

            if (listValueObj is not System.Collections.IEnumerable enumerable)
                throw new NotSupportedException("IN list must be an IEnumerable.");

            var values = new List<object?>();
            foreach (var v in enumerable)
                values.Add(v);

            if (values.Count == 0)
                throw new InvalidOperationException("IN list is empty. Business logic must not pass an empty collection.");

            if (values.Count > MaxInListSize)
                throw new InvalidOperationException($"IN list is too large (count={values.Count}). Business logic must not pass a large collection.");

            var colSql = Visit(valueExpr, parameters, VisitContext.Column);
            var paramSqls = values.Select(v => AddParam(parameters, v)).ToList();
            sql = $"({colSql} IN ({string.Join(", ", paramSqls)}))";
            return true;
        }

        private static bool IsColumnAccess(Expression expression)
        {
            expression = ExpressionHelpers.StripConvert(expression);
            return expression is MemberExpression me && me.Expression is ParameterExpression;
        }

        private static bool IsSqlScalarExpression(Expression expression)
        {
            expression = ExpressionHelpers.StripConvert(expression);

            if (expression is ConstantExpression)
                return true;
            if (IsColumnAccess(expression))
                return true;
            if (expression is BinaryExpression be)
            {
                return be.NodeType == ExpressionType.Add || be.NodeType == ExpressionType.Subtract ||
                       be.NodeType == ExpressionType.Multiply || be.NodeType == ExpressionType.Divide ||
                       be.NodeType == ExpressionType.Modulo;
            }
            if (expression is MethodCallExpression mce && mce.Method.DeclaringType == typeof(SqlFn))
                return true;
            return false;
        }

        private string AddParam(DynamicParameters parameters, object? value)
        {
            var name = $"p{_pIndex++}";
            parameters.Add(name, value);
            return $"@{name}";
        }

        private enum VisitContext
        {
            Predicate,
            Column
        }
    }

    internal static class MappingUtils
    {
        public static TableMapping GetOrAddMapping(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return DBModelAnalysisContext.Mappings.GetOrAdd(type.FullName!, _ => Build(type));
        }

        public static string GetColumnName(PropertyInfo property)
        {
            var fieldInfo = property.GetCustomAttribute<DBFieldAttribute>(true);
            if (fieldInfo != null && !string.IsNullOrWhiteSpace(fieldInfo.FieldName))
                return fieldInfo.FieldName;
            return property.Name;
        }

        private static TableMapping Build(Type type)
        {
            TableMapping tbMapping = new TableMapping();
            var tableAttr = type.GetCustomAttribute<DataTableAttribute>();
            tbMapping.TableName = tableAttr == null ? type.Name : tableAttr.TableName;

            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.CustomAttributes.Any(x => x.AttributeType == typeof(DBFieldNotMappedAttribute)) ||
                     prop.PropertyType.Name.StartsWith("List"))
                {
                    continue;
                }
                var propMapping = new PropMapping() { PropertyName = prop.Name, PropertyType = prop.PropertyType };
                var fieldInfo = prop.GetCustomAttribute<DBFieldAttribute>(true);
                if (fieldInfo != null)
                {
                    propMapping.IsKey = fieldInfo.IsKeyField;
                    propMapping.FieldName = fieldInfo.FieldName;
                    propMapping.AutoGenerate = fieldInfo.AutoGenerate;
                    if (fieldInfo.Groups != null)
                    {
                        propMapping.FieldGroups = fieldInfo.Groups.ToList();
                    }
                }
                tbMapping.PropMappings.Add(propMapping);
            }

            return tbMapping;
        }
    }

    internal static class ExpressionHelpers
    {
        public static Expression StripConvert(Expression expression)
        {
            while (expression is UnaryExpression ue && (ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked))
                expression = ue.Operand;
            return expression;
        }

        public static bool IsNullConstant(Expression expression)
        {
            expression = StripConvert(expression);
            return expression is ConstantExpression ce && ce.Value == null;
        }

        public static object? GetValue(Expression expression)
        {
            expression = StripConvert(expression);
            if (expression is ConstantExpression ce)
                return ce.Value;
            return Expression.Lambda(Expression.Convert(expression, typeof(object))).Compile().DynamicInvoke();
        }

        public static string GetPropertyName(LambdaExpression propertyExpr)
        {
            var body = StripConvert(propertyExpr.Body);
            if (body is MemberExpression me && me.Member is PropertyInfo pi)
                return pi.Name;

            throw new ArgumentException("Expression must be a property access.", nameof(propertyExpr));
        }
    }
}
