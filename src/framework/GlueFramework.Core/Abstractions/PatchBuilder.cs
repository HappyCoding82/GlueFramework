using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GlueFramework.Core.Abstractions
{
    public sealed class PatchBuilder<T>
    {
        private readonly Dictionary<string, object?> _changes = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, object?> Changes => _changes;

        public PatchBuilder<T> Set(string name, object? value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field name is required.", nameof(name));

            _changes[name] = value;
            return this;
        }

        public PatchBuilder<T> Set<TValue>(Expression<Func<T, TValue>> property, TValue value)
        {
            var name = GetPropertyName(property);
            _changes[name] = value;
            return this;
        }

        private static string GetPropertyName<TValue>(Expression<Func<T, TValue>> expr)
        {
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            Expression body = expr.Body;
            if (body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
                body = ue.Operand;

            if (body is not MemberExpression me)
                throw new ArgumentException("Expression must be a property access.", nameof(expr));

            return me.Member.Name;
        }
    }
}
