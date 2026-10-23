using GlueFramework.Core.Abstractions;
using GlueFramework.Core.LambdaToSQL.Extensions;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GlueFramework.Core.ORM.LambdaToSQL
{
    public static class WhereBuilder
    {
        private static readonly object _lockObject = new object();
        public static string TableName { get; set; } = string.Empty;
        private static readonly IDictionary<ExpressionType, string> nodeTypeMappings = new Dictionary<ExpressionType, string>
        {
            {ExpressionType.Add, "+"},
            {ExpressionType.And, "AND"},
            {ExpressionType.AndAlso, "AND"},
            {ExpressionType.Divide, "/"},
            {ExpressionType.Equal, "="},
            {ExpressionType.ExclusiveOr, "^"},
            {ExpressionType.GreaterThan, ">"},
            {ExpressionType.GreaterThanOrEqual, ">="},
            {ExpressionType.LessThan, "<"},
            {ExpressionType.LessThanOrEqual, "<="},
            {ExpressionType.Modulo, "%"},
            {ExpressionType.Multiply, "*"},
            {ExpressionType.Negate, "-"},
            {ExpressionType.Not, "NOT"},
            {ExpressionType.NotEqual, "<>"},
            {ExpressionType.Or, "OR"},
            {ExpressionType.OrElse, "OR"},
            {ExpressionType.Subtract, "-"}
        };

        private static SqlBuilderFactory.DBTypes _dbType = SqlBuilderFactory.DBTypes.SQLSERVER;
        public static WherePart ToSql<T>(this Expression<Func<T, bool>> expression, string tbName, SqlBuilderFactory.DBTypes dbType = SqlBuilderFactory.DBTypes.SQLSERVER)
        {
            lock (_lockObject)
            {
                var i = 1;
                _dbType = dbType;
                TableName = tbName;
                var result = Recurse<T>(ref i, expression.Body, isUnary: true);
                return result;
            }
        }

        private static WherePart Recurse<T>(ref int i, Expression expression, bool isUnary = false,
            string prefix = null, string postfix = null, bool left = true)
        {
            switch (expression)
            {
                case UnaryExpression unary: return UnaryExpressionExtract<T>(ref i, unary);
                case BinaryExpression binary: return BinaryExpressionExtract<T>(ref i, binary);
                case ConstantExpression constant: return ConstantExpressionExtract(ref i, constant, isUnary, prefix, postfix, left);
                case MemberExpression member: return MemberExpressionExtract<T>(ref i, member, isUnary, prefix, postfix, left);
                case MethodCallExpression method: return MethodCallExpressionExtract<T>(ref i, method);
                case InvocationExpression invocation: return InvocationExpressionExtract<T>(ref i, invocation, left);
                default: throw new Exception("Unsupported expression: " + expression.GetType().Name);
            }
        }

        private static WherePart InvocationExpressionExtract<T>(ref int i, InvocationExpression expression, bool left)
        {
            return Recurse<T>(ref i, ((Expression<Func<T, bool>>)expression.Expression).Body, left: left);
        }

        private static WherePart MethodCallExpressionExtract<T>(ref int i, MethodCallExpression expression)
        {
            // LIKE queries:
            if (expression.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
            {
                var likeOp = _dbType == SqlBuilderFactory.DBTypes.POSTGRESQL ? "ILIKE" : "LIKE";
                return WherePart.Concat(Recurse<T>(ref i, expression.Object), likeOp,
                    Recurse<T>(ref i, expression.Arguments[0], prefix: "%", postfix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
            {
                var likeOp = _dbType == SqlBuilderFactory.DBTypes.POSTGRESQL ? "ILIKE" : "LIKE";
                return WherePart.Concat(Recurse<T>(ref i, expression.Object), likeOp,
                    Recurse<T>(ref i, expression.Arguments[0], postfix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
            {
                var likeOp = _dbType == SqlBuilderFactory.DBTypes.POSTGRESQL ? "ILIKE" : "LIKE";
                return WherePart.Concat(Recurse<T>(ref i, expression.Object), likeOp,
                    Recurse<T>(ref i, expression.Arguments[0], prefix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("Equals", new[] { typeof(string) }))
            {
                return WherePart.Concat(Recurse<T>(ref i, expression.Object), "=",
                    Recurse<T>(ref i, expression.Arguments[0], left: false));
            }

            // IN queries:
            if (expression.Method.Name == "Contains")
            {
                Expression collection;
                Expression property;
                if (expression.Method.IsDefined(typeof(ExtensionAttribute)) && expression.Arguments.Count == 2)
                {
                    collection = expression.Arguments[0];
                    property = expression.Arguments[1];
                }
                else if (!expression.Method.IsDefined(typeof(ExtensionAttribute)) && expression.Arguments.Count == 1)
                {
                    collection = expression.Object;
                    property = expression.Arguments[0];
                }
                else
                {
                    throw new Exception("Unsupported method call: " + expression.Method.Name);
                }

                var values = (IEnumerable)GetValue(collection);
                return WherePart.Concat(Recurse<T>(ref i, property), "IN", WherePart.IsCollection(ref i, values));
            }

            throw new Exception("Unsupported method call: " + expression.Method.Name);
        }

        private static WherePart MemberExpressionExtract<T>(ref int i, MemberExpression expression, bool isUnary,
            string prefix, string postfix, bool left)
        {
            if (isUnary && expression.Type == typeof(bool))
            {
                if (_dbType == SqlBuilderFactory.DBTypes.POSTGRESQL)
                    return WherePart.Concat(Recurse<T>(ref i, expression), "=", WherePart.IsSql("TRUE"));

                return WherePart.Concat(Recurse<T>(ref i, expression), "=", WherePart.IsSql("1"));
            }

            if (expression.Member is PropertyInfo property)
            {
                if (left)
                {
                    var colName = GetName<DBFieldAttribute>(property);
                    
                    if (_dbType == SqlBuilderFactory.DBTypes.MYSQL)
                        return WherePart.IsSql($"{TableName}.`{colName}`");
                    else if (_dbType == SqlBuilderFactory.DBTypes.POSTGRESQL)
                        return WherePart.IsSql($"{TableName}.\"{colName}\"");
                    else
                        return WherePart.IsSql($"{TableName}.[{colName}]");
                }

                if (property.PropertyType == typeof(bool))
                {
                    var colName = GetName<DBFieldAttribute>(property);
                    
                    if (_dbType == SqlBuilderFactory.DBTypes.MYSQL)
                        return WherePart.IsSql($"{TableName}.`{colName}`=1");
                    else if (_dbType == SqlBuilderFactory.DBTypes.POSTGRESQL)
                        return WherePart.IsSql($"{TableName}.\"{colName}\"=TRUE");
                    else
                        return WherePart.IsSql($"{TableName}.[{colName}]=1");
                }
            }

            if (expression.Member is FieldInfo || left == false)
            {
                var value = GetValue(expression);
                if (value is string textValue)
                {
                    value = prefix + textValue + postfix;
                }

                return WherePart.IsParameter(i++, value);
            }

            throw new Exception($"Expression does not refer to a property or field: {expression}");
        }

        private static string GetName<T>(PropertyInfo pi) where T : IAttributeName
        {
            if (Configuration.GetInstance().Properties() != null)
            {
                var result = Configuration.GetInstance().Properties().FirstOrDefault(p => p.Type() == pi);
                if (result != null)
                    return result.GetColumnName();
            }

            var attributes = pi.GetCustomAttributes(typeof(T), false).AsList();
            if (attributes.Count != 1) return pi.Name;

            var attributeName = (T)attributes[0];
            return attributeName.GetName();
        }

        //private static string GetName<T>(Type type) where T : IAttributeName
        //{
        //    if (Configuration.GetInstance().Entities() != null)
        //    {
        //        var result = Configuration.GetInstance()
        //            .Entities()
        //            .FirstOrDefault(p => p.Name().Equals(type.Name, StringComparison.CurrentCultureIgnoreCase));
        //        if (result != null)
        //            return TablePrefix + result.GetTableName();
        //    }

        //    var attributes = type.GetCustomAttributes(typeof(T), false).AsList();
        //    if (attributes.Count != 1) return type.Name;

        //    var attributeName = (T)attributes[0];
        //    return TablePrefix + attributeName.GetName();
        //}

        private static WherePart ConstantExpressionExtract(ref int i, ConstantExpression expression, bool isUnary,
            string prefix, string postfix, bool left)
        {
            var value = expression.Value;

            switch (value)
            {
                case null:
                    return WherePart.IsSql("NULL");
                case int _:
                    return WherePart.IsSql(value.ToString());
                case string text:
                    value = prefix + text + postfix;
                    break;
            }

            if (!(value is bool boolValue) || isUnary) return WherePart.IsParameter(i++, value);

            string result;
            if (left)
            {
                if (_dbType == SqlBuilderFactory.DBTypes.POSTGRESQL)
                    result = boolValue ? "TRUE" : "FALSE";
                else
                    result = boolValue ? "1=1" : "0=1";
            }
            else
            {
                if (_dbType == SqlBuilderFactory.DBTypes.POSTGRESQL)
                    result = boolValue ? "TRUE" : "FALSE";
                else
                    result = boolValue ? "1" : "0";
            }

            return WherePart.IsSql(result);
        }

        private static WherePart BinaryExpressionExtract<T>(ref int i, BinaryExpression expression)
        {
            return WherePart.Concat(Recurse<T>(ref i, expression.Left), NodeTypeToString(expression.NodeType),
                Recurse<T>(ref i, expression.Right, left: false));
        }

        private static WherePart UnaryExpressionExtract<T>(ref int i, UnaryExpression expression)
        {
            return WherePart.Concat(NodeTypeToString(expression.NodeType), Recurse<T>(ref i, expression.Operand, true));
        }

        private static object GetValue(Expression member)
        {
            return Evaluate(member);
        }

        private static object Evaluate(Expression expression)
        {
            if (expression is ConstantExpression constant)
            {
                return constant.Value;
            }

            if (expression is MemberExpression memberExpression)
            {
                var target = memberExpression.Expression != null
                    ? Evaluate(memberExpression.Expression)
                    : null;

                return memberExpression.Member switch
                {
                    FieldInfo field => field.GetValue(target),
                    PropertyInfo property => property.GetValue(target),
                    _ => throw new NotSupportedException($"Unsupported member type: {memberExpression.Member.GetType()}")
                };
            }

            // C# 14/.NET 10 compilers optimize array .Contains() into a span-based call
            // (System.MemoryExtensions.Contains), producing an implicit-conversion wrapper
            // op_Implicit(Convert(<source>, T[])). ReadOnlySpan<T> is a ref struct and cannot
            // be boxed, so unwrap the argument and evaluate the real collection instead of
            // falling through to Expression.Convert (which would throw InvalidProgramException).
            if (expression is MethodCallExpression methodCall
                && methodCall.Method.Name == "op_Implicit"
                && methodCall.Arguments.Count >= 1)
            {
                return Evaluate(methodCall.Arguments[0]);
            }

            // Fallback for expressions not handled above (e.g. NewArrayExpression)
            var objectMember = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private static string NodeTypeToString(ExpressionType nodeType)
        {
            return nodeTypeMappings.TryGetValue(nodeType, out var value)
                ? value
                : string.Empty;
        }

        public static List<T> AsList<T>(this IEnumerable<T> source) =>
            source == null || source is List<T> ? (List<T>)source : source.ToList();
    }
}