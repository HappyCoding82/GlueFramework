using System.Collections.Generic;
using System.Linq.Expressions;

namespace GlueFramework.Core.Abstractions
{
    public class FilterOptions
    {
        public List<string> OrderByStatements { get; set; } = new List<string>();
        public int? TakeNumber { get; set; }

        public string WhereClause { get; set; }

        public PagerInfo Pager { get; set; }
    }

    public class FilterOptions<Model>
    {
        public FilterOptions( Expression<Func<Model, bool>> whereClause, PagerInfo pager, List<string> orderByStatements = null)
        {
            WhereClause = whereClause;
            Pager = pager;
            if(orderByStatements != null)
                OrderByStatements = orderByStatements;
        }

        public List<string> OrderByStatements { get; set; } = new List<string>();
        public int? TakeNumber { get; set; }

        public Expression<Func<Model, bool>> WhereClause { get; set; } 

        public PagerInfo Pager { get; set; }
    }
}
