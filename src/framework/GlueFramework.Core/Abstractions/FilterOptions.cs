using System.Linq.Expressions;

namespace GlueFramework.Core.Abstractions
{
    public sealed class OrderByExpression<Model>
    {
        public required Expression<Func<Model, object?>> KeySelector { get; init; }
        public bool Desc { get; init; }
    }

    public sealed class PagedFilterOptions<Model>
    {
        public PagedFilterOptions(
            Expression<Func<Model, bool>> whereClause,
            PagerInfo pager,
            Expression<Func<Model, object?>> orderBy,
            bool desc = false)
        {
            if (whereClause == null)
                throw new System.ArgumentNullException(nameof(whereClause));
            if (pager == null)
                throw new System.ArgumentNullException(nameof(pager));
            if (orderBy == null)
                throw new System.ArgumentNullException(nameof(orderBy));

            WhereClause = whereClause;
            Pager = pager;
            OrderByExpressions.Add(new OrderByExpression<Model> { KeySelector = orderBy, Desc = desc });
        }

        public List<OrderByExpression<Model>> OrderByExpressions { get; } = new List<OrderByExpression<Model>>();

        public Expression<Func<Model, bool>> WhereClause { get; set; }

        public PagerInfo Pager { get; set; }

        public PagedFilterOptions<Model> ThenBy(Expression<Func<Model, object?>> keySelector)
        {
            if (keySelector == null)
                throw new System.ArgumentNullException(nameof(keySelector));

            OrderByExpressions.Add(new OrderByExpression<Model> { KeySelector = keySelector, Desc = false });
            return this;
        }

        public PagedFilterOptions<Model> ThenByDescending(Expression<Func<Model, object?>> keySelector)
        {
            if (keySelector == null)
                throw new System.ArgumentNullException(nameof(keySelector));

            OrderByExpressions.Add(new OrderByExpression<Model> { KeySelector = keySelector, Desc = true });
            return this;
        }
    }
}
