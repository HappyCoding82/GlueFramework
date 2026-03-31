using System.Linq.Expressions;

namespace GlueFramework.Core.Extensions
{
    public static class PredicateExpressionExtensions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var p = left.Parameters[0];
            var rightBody = new ReplaceParamsVisitor(right.Parameters[0], p).Visit(right.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left.Body, rightBody!),
                p);
        }

        public static Expression<Func<T1, T2, bool>> AndAlso<T1, T2>(
            this Expression<Func<T1, T2, bool>> left,
            Expression<Func<T1, T2, bool>> right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var p1 = left.Parameters[0];
            var p2 = left.Parameters[1];

            var rightBody = new ReplaceParamsVisitor(
                (right.Parameters[0], p1),
                (right.Parameters[1], p2)).Visit(right.Body);

            return Expression.Lambda<Func<T1, T2, bool>>(
                Expression.AndAlso(left.Body, rightBody!),
                p1, p2);
        }

        public static Expression<Func<T1, T2, T3, bool>> AndAlso<T1, T2, T3>(
            this Expression<Func<T1, T2, T3, bool>> left,
            Expression<Func<T1, T2, T3, bool>> right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var p1 = left.Parameters[0];
            var p2 = left.Parameters[1];
            var p3 = left.Parameters[2];

            var rightBody = new ReplaceParamsVisitor(
                (right.Parameters[0], p1),
                (right.Parameters[1], p2),
                (right.Parameters[2], p3)).Visit(right.Body);

            return Expression.Lambda<Func<T1, T2, T3, bool>>(
                Expression.AndAlso(left.Body, rightBody!),
                p1, p2, p3);
        }

        public static Expression<Func<T, bool>> OrElse<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var p = left.Parameters[0];
            var rightBody = new ReplaceParamsVisitor(right.Parameters[0], p).Visit(right.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(left.Body, rightBody!),
                p);
        }

        public static Expression<Func<T1, T2, bool>> OrElse<T1, T2>(
            this Expression<Func<T1, T2, bool>> left,
            Expression<Func<T1, T2, bool>> right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var p1 = left.Parameters[0];
            var p2 = left.Parameters[1];

            var rightBody = new ReplaceParamsVisitor(
                (right.Parameters[0], p1),
                (right.Parameters[1], p2)).Visit(right.Body);

            return Expression.Lambda<Func<T1, T2, bool>>(
                Expression.OrElse(left.Body, rightBody!),
                p1, p2);
        }

        public static Expression<Func<T1, T2, T3, bool>> OrElse<T1, T2, T3>(
            this Expression<Func<T1, T2, T3, bool>> left,
            Expression<Func<T1, T2, T3, bool>> right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var p1 = left.Parameters[0];
            var p2 = left.Parameters[1];
            var p3 = left.Parameters[2];

            var rightBody = new ReplaceParamsVisitor(
                (right.Parameters[0], p1),
                (right.Parameters[1], p2),
                (right.Parameters[2], p3)).Visit(right.Body);

            return Expression.Lambda<Func<T1, T2, T3, bool>>(
                Expression.OrElse(left.Body, rightBody!),
                p1, p2, p3);
        }

        private sealed class ReplaceParamsVisitor : ExpressionVisitor
        {
            private readonly (ParameterExpression from, ParameterExpression to)[] _map;

            public ReplaceParamsVisitor(ParameterExpression from, ParameterExpression to)
                : this((from, to))
            {
            }

            public ReplaceParamsVisitor(params (ParameterExpression from, ParameterExpression to)[] map)
            {
                _map = map ?? throw new ArgumentNullException(nameof(map));
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                for (int i = 0; i < _map.Length; i++)
                {
                    if (node == _map[i].from)
                        return _map[i].to;
                }
                return base.VisitParameter(node);
            }
        }
    }
}
