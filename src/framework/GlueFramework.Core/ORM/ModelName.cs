using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace GlueFramework.Core.ORM
{
    public static class ModelName
    {
        private static readonly ConcurrentDictionary<Type, string> TableNameCache = new();
        private static readonly ConcurrentDictionary<(Type ModelType, string PropertyName), string> ColumnNameCache = new();

        public static string Table<T>() where T : class
        {
            return TableNameCache.GetOrAdd(typeof(T), static t =>
            {
                var attr = t.GetCustomAttribute<DataTableAttribute>(inherit: true);
                if (attr != null && !string.IsNullOrWhiteSpace(attr.TableName))
                    return attr.TableName;
                return t.Name;
            });
        }

        public static string Column<T>(Expression<Func<T, object?>> property) where T : class
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var member = GetMemberExpression(property.Body);
            if (member.Member is not PropertyInfo pi)
                throw new ArgumentException($"Expression must be a property access, got: {property}", nameof(property));

            return Column<T>(pi.Name);
        }

        public static string Column<T>(string propertyName) where T : class
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name must not be null or whitespace.", nameof(propertyName));

            return ColumnNameCache.GetOrAdd((typeof(T), propertyName), static key =>
            {
                var (modelType, propName) = key;
                var pi = modelType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi == null)
                    return propName;

                var attr = pi.GetCustomAttribute<DBFieldAttribute>(inherit: true);
                if (attr != null && !string.IsNullOrWhiteSpace(attr.FieldName))
                    return attr.FieldName;

                return pi.Name;
            });
        }

        private static MemberExpression GetMemberExpression(Expression body)
        {
            if (body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
                body = ue.Operand;

            if (body is MemberExpression me)
                return me;

            throw new ArgumentException($"Expression must be a member access, got: {body}", nameof(body));
        }
    }
}
