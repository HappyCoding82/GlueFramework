using GlueFramework.Core.Abstractions;
using GlueFramework.Core.ORM;
using GlueFramework.Core.Services;

namespace GlueFramework.CoreTests.Sql
{
    internal sealed class SqlGenerationTestService : ServiceBase
    {
        public SqlGenerationTestService(IDbConnectionAccessor dbConnectionAccessor, IDataTablePrefixProvider dataTablePrefixProvider)
            : base(dbConnectionAccessor, dataTablePrefixProvider)
        {
        }

        public JoinQuerySqlResult BuildDemoProductReportSql(string? name, int skip, int take)
        {
            using var s = OpenJoinQuerySessionScope();

            var q = s.Session
                .From<DemoProduct>()
                .Join<Category>((p, c) => p.CategoryId == c.Id)
                .Join<Brand>((p, c, b) => p.BrandId == b.Id, GlueFramework.Core.ORM.JoinType.Left);

            if (!string.IsNullOrWhiteSpace(name))
            {
                q = q.Where((p, c, b) => p.Name.Contains(name));
            }

            var task = q
                .OrderBy((p, c, b) => p.Id)
                .Select((p, c, b) => new DemoProductReportRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Price = p.Price,
                    CategoryName = c.CategoryName,
                    BrandName = b.BrandName
                })
                .Page(skip, take)
                .PrintToListSqlAsync();

            var (sql, parameters) = task.GetAwaiter().GetResult();
            return new JoinQuerySqlResult(sql, parameters.ParameterNames.ToArray());
        }

        public JoinQuerySqlResult BuildDemoProductReportSql(
            System.Linq.Expressions.Expression<Func<DemoProduct, Category, Brand, bool>> where,
            int skip,
            int take)
        {
            using var s = OpenJoinQuerySessionScope();

            var q = s.Session
                .From<DemoProduct>()
                .Join<Category>((p, c) => p.CategoryId == c.Id)
                .Join<Brand>((p, c, b) => p.BrandId == b.Id, GlueFramework.Core.ORM.JoinType.Left)
                .Where(where);

            var task = q
                .OrderBy((p, c, b) => p.Id)
                .Select((p, c, b) => new DemoProductReportRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Price = p.Price,
                    CategoryName = c.CategoryName,
                    BrandName = b.BrandName
                })
                .Page(skip, take)
                .PrintToListSqlAsync();

            var (sql, parameters) = task.GetAwaiter().GetResult();
            return new JoinQuerySqlResult(sql, parameters.ParameterNames.ToArray());
        }

        public JoinQuerySqlResult BuildDemoProductReportSqlWithIn(
            IReadOnlyCollection<int> ids,
            int skip,
            int take)
        {
            using var s = OpenJoinQuerySessionScope();

            var q = s.Session
                .From<DemoProduct>()
                .Join<Category>((p, c) => p.CategoryId == c.Id)
                .Join<Brand>((p, c, b) => p.BrandId == b.Id, GlueFramework.Core.ORM.JoinType.Left)
                .Where((p, c, b) => ids.Contains(p.Id));

            var task = q
                .OrderBy((p, c, b) => p.Id)
                .Select((p, c, b) => new DemoProductReportRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Price = p.Price,
                    CategoryName = c.CategoryName,
                    BrandName = b.BrandName
                })
                .Page(skip, take)
                .PrintToListSqlAsync();

            var (sql, parameters) = task.GetAwaiter().GetResult();
            return new JoinQuerySqlResult(sql, parameters.ParameterNames.ToArray());
        }

        public JoinQuerySqlResult BuildDemoProductReportSqlWithIn_Empty(int skip, int take)
        {
            return BuildDemoProductReportSqlWithIn(Array.Empty<int>(), skip, take);
        }

        public JoinQuerySqlResult BuildDemoProductReportSqlWithIn_TooLarge(int skip, int take)
        {
            var ids = Enumerable.Range(1, 1001).ToArray();
            return BuildDemoProductReportSqlWithIn(ids, skip, take);
        }

        public JoinQuerySqlResult BuildDemoProductAggregateSql(decimal minTotal, bool distinct)
        {
            using var s = OpenJoinQuerySessionScope();

            var q = s.Session
                .From<DemoProduct>()
                .Join<Category>((p, c) => p.CategoryId == c.Id);

            var sel = q
                .Select((p, c) => new DemoProductAggregateRow
                {
                    CategoryId = p.CategoryId,
                    Total = SqlFn.Sum(p.Price * p.Qty)
                })
                .GroupBy((p, c) => new { p.CategoryId })
                .Having((p, c) => SqlFn.Sum(p.Price * p.Qty) > minTotal);

            if (distinct)
                sel = sel.Distinct();

            var task = sel.PrintToListSqlAsync();
            var (sql, parameters) = task.GetAwaiter().GetResult();
            return new JoinQuerySqlResult(sql, parameters.ParameterNames.ToArray());
        }

        public JoinQuerySqlResult BuildDemoProductCalcSql()
        {
            using var s = OpenJoinQuerySessionScope();

            var q = s.Session
                .From<DemoProduct>()
                .Join<Category>((p, c) => p.CategoryId == c.Id);

            var task = q
                .Select((p, c) => new DemoProductCalcRow
                {
                    AA = p.Price * p.Qty,
                    Qty = p.Qty
                })
                .PrintToListSqlAsync();

            var (sql, parameters) = task.GetAwaiter().GetResult();
            return new JoinQuerySqlResult(sql, parameters.ParameterNames.ToArray());
        }
    }

    internal readonly record struct JoinQuerySqlResult(string Sql, string[] ParameterNames);
}
