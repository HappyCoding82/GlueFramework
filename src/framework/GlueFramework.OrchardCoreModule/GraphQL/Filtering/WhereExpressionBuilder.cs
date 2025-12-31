using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GlueFramework.OrchardCoreModule.GraphQL.Filtering
{
    public static class WhereExpressionBuilder
    {
        public static Expression<Func<T, bool>> Build<T>(Dictionary<string, object?>? where)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var body = BuildBody<T>(where, param) ?? Expression.Constant(true);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static Expression? BuildBody<T>(Dictionary<string, object?>? where, ParameterExpression param)
        {
            if (where == null || where.Count == 0)
                return null;

            Expression? andBody = null;

            if (TryGetList(where, "and", out var andList))
            {
                foreach (var item in andList)
                {
                    if (item is Dictionary<string, object?> dict)
                    {
                        var child = BuildBody<T>(dict, param);
                        andBody = AndAlso(andBody, child);
                    }
                }
            }

            if (TryGetList(where, "or", out var orList))
            {
                Expression? orBody = null;
                foreach (var item in orList)
                {
                    if (item is Dictionary<string, object?> dict)
                    {
                        var child = BuildBody<T>(dict, param);
                        orBody = OrElse(orBody, child);
                    }
                }

                andBody = AndAlso(andBody, orBody);
            }

            foreach (var kv in where)
            {
                var key = kv.Key;
                if (key.Equals("and", StringComparison.OrdinalIgnoreCase) || key.Equals("or", StringComparison.OrdinalIgnoreCase))
                    continue;

                var prop = ResolveProperty(typeof(T), key);
                if (prop == null)
                    continue;

                if (kv.Value is not Dictionary<string, object?> ops)
                    continue;

                var member = Expression.Property(param, prop);
                var predicate = BuildOpsPredicate(member, prop.PropertyType, ops);
                andBody = AndAlso(andBody, predicate);
            }

            return andBody;
        }

        private static PropertyInfo? ResolveProperty(Type t, string graphqlFieldName)
        {
            var pascal = char.ToUpperInvariant(graphqlFieldName[0]) + graphqlFieldName.Substring(1);
            return t.GetProperty(pascal, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        }

        private static Expression? BuildOpsPredicate(Expression member, Type memberType, Dictionary<string, object?> ops)
        {
            var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;
            Expression? body = null;

            foreach (var op in ops)
            {
                if (op.Value == null)
                    continue;

                switch (op.Key)
                {
                    case "eq":
                        body = AndAlso(body, Expression.Equal(EnsureType(member, underlying), ToConstant(op.Value, underlying)));
                        break;
                    case "gt":
                        body = AndAlso(body, Expression.GreaterThan(EnsureType(member, underlying), ToConstant(op.Value, underlying)));
                        break;
                    case "gte":
                        body = AndAlso(body, Expression.GreaterThanOrEqual(EnsureType(member, underlying), ToConstant(op.Value, underlying)));
                        break;
                    case "lt":
                        body = AndAlso(body, Expression.LessThan(EnsureType(member, underlying), ToConstant(op.Value, underlying)));
                        break;
                    case "lte":
                        body = AndAlso(body, Expression.LessThanOrEqual(EnsureType(member, underlying), ToConstant(op.Value, underlying)));
                        break;
                    case "contains":
                        body = AndAlso(body, CallString(member, nameof(string.Contains), op.Value));
                        break;
                    case "startsWith":
                        body = AndAlso(body, CallString(member, nameof(string.StartsWith), op.Value));
                        break;
                    case "endsWith":
                        body = AndAlso(body, CallString(member, nameof(string.EndsWith), op.Value));
                        break;
                    case "in":
                        body = AndAlso(body, BuildInPredicate(member, underlying, op.Value));
                        break;
                }
            }

            return body;
        }

        private static Expression EnsureType(Expression member, Type underlying)
        {
            if (member.Type == underlying)
                return member;

            return Expression.Convert(member, underlying);
        }

        private static Expression ToConstant(object value, Type target)
        {
            if (value.GetType() == target)
                return Expression.Constant(value, target);

            var converted = Convert.ChangeType(value, target);
            return Expression.Constant(converted, target);
        }

        private static Expression? CallString(Expression member, string method, object value)
        {
            if (value is not string s)
                return null;

            var instance = EnsureType(member, typeof(string));
            var mi = typeof(string).GetMethod(method, new[] { typeof(string) });
            return mi == null ? null : Expression.Call(instance, mi, Expression.Constant(s));
        }

        private static Expression? BuildInPredicate(Expression member, Type underlying, object value)
        {
            if (value is not IEnumerable enumerable)
                return null;

            var list = new List<object>();
            foreach (var x in enumerable)
            {
                if (x != null)
                    list.Add(x);
            }

            if (list.Count == 0)
                return null;

            var array = Array.CreateInstance(underlying, list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                array.SetValue(Convert.ChangeType(list[i], underlying), i);
            }

            var contains = typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(underlying);

            return Expression.Call(
                contains,
                Expression.Constant(array),
                EnsureType(member, underlying));
        }

        private static bool TryGetList(Dictionary<string, object?> dict, string key, out IEnumerable<object?> list)
        {
            list = Array.Empty<object?>();
            if (!dict.TryGetValue(key, out var raw) || raw == null)
                return false;

            if (raw is IEnumerable<object?> typed)
            {
                list = typed;
                return true;
            }

            if (raw is IEnumerable enumerable)
            {
                var tmp = new List<object?>();
                foreach (var x in enumerable)
                    tmp.Add(x);
                list = tmp;
                return true;
            }

            return false;
        }

        private static Expression? AndAlso(Expression? left, Expression? right)
        {
            if (left == null) return right;
            if (right == null) return left;
            return Expression.AndAlso(left, right);
        }

        private static Expression? OrElse(Expression? left, Expression? right)
        {
            if (left == null) return right;
            if (right == null) return left;
            return Expression.OrElse(left, right);
        }
    }
}
