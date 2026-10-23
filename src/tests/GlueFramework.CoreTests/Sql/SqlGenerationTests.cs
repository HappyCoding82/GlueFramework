using GlueFramework.Core.Abstractions;
using GlueFramework.Core.ORM;
using GlueFramework.Core.ORM.LambdaToSQL;
using GlueFramework.Core.LambdaToSQL.Extensions;
using System.Linq.Expressions;
using static GlueFramework.Core.ORM.SqlBuilderFactory;

namespace GlueFramework.CoreTests.Sql
{
    [TestClass]
    public sealed class SqlGenerationTests
    {
        private sealed class FixedPrefixProvider : IDataTablePrefixProvider
        {
            public FixedPrefixProvider(string prefix) { Prefix = prefix; }
            public string Prefix { get; }
        }

        private sealed class FixedTenantTableSettingsProvider : ITenantTableSettingsProvider
        {
            public FixedTenantTableSettingsProvider(string prefix, string? schema)
            {
                Prefix = prefix;
                Schema = schema;
            }

            public string Prefix { get; }

            public string? Schema { get; }
        }

        [TestMethod]
        public void DemoProductReport_GeneratesSqlServerSql_WithPagingAndLike()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(name: "abc", skip: 10, take: 20);

            const string expected =
                "SELECT t1.[Id] AS [ProductId], t1.[Name] AS [ProductName], t1.[Price] AS [Price], t2.[CategoryName] AS [CategoryName], t3.[BrandName] AS [BrandName] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]) LEFT JOIN [Brand] t3 ON (t1.[BrandId] = t3.[Id]) " +
                "WHERE (t1.[Name] LIKE @p1) " +
                "ORDER BY t1.[Id] ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void DemoProductReport_GeneratesSqlServerSql_WithSchemaAndPrefix()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedTenantTableSettingsProvider(prefix: "s_", schema: "dbo"));

            var result = service.BuildDemoProductReportSql(name: "abc", skip: 10, take: 20);

            const string expected =
                "SELECT t1.[Id] AS [ProductId], t1.[Name] AS [ProductName], t1.[Price] AS [Price], t2.[CategoryName] AS [CategoryName], t3.[BrandName] AS [BrandName] " +
                "FROM [dbo].[s_DemoProduct] t1 JOIN [dbo].[s_Category] t2 ON (t1.[CategoryId] = t2.[Id]) LEFT JOIN [dbo].[s_Brand] t3 ON (t1.[BrandId] = t3.[Id]) " +
                "WHERE (t1.[Name] LIKE @p1) " +
                "ORDER BY t1.[Id] ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void DemoProductReport_GeneratesPostgreSql_WithSchemaAndPrefix()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedTenantTableSettingsProvider(prefix: "s_", schema: "public"));

            var result = service.BuildDemoProductReportSql(name: "abc", skip: 10, take: 20);

            const string expected =
                "SELECT t1.\"Id\" AS \"ProductId\", t1.\"Name\" AS \"ProductName\", t1.\"Price\" AS \"Price\", t2.\"CategoryName\" AS \"CategoryName\", t3.\"BrandName\" AS \"BrandName\" " +
                "FROM \"public\".\"s_DemoProduct\" t1 JOIN \"public\".\"s_Category\" t2 ON (t1.\"CategoryId\" = t2.\"Id\") LEFT JOIN \"public\".\"s_Brand\" t3 ON (t1.\"BrandId\" = t3.\"Id\") " +
                "WHERE (t1.\"Name\" ILIKE @p1) " +
                "ORDER BY t1.\"Id\" ASC LIMIT @take OFFSET @skip;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void DemoProductReport_GeneratesPostgreSql_WithPagingAndILike()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(name: "abc", skip: 10, take: 20);

            const string expected =
                "SELECT t1.\"Id\" AS \"ProductId\", t1.\"Name\" AS \"ProductName\", t1.\"Price\" AS \"Price\", t2.\"CategoryName\" AS \"CategoryName\", t3.\"BrandName\" AS \"BrandName\" " +
                "FROM \"DemoProduct\" t1 JOIN \"Category\" t2 ON (t1.\"CategoryId\" = t2.\"Id\") LEFT JOIN \"Brand\" t3 ON (t1.\"BrandId\" = t3.\"Id\") " +
                "WHERE (t1.\"Name\" ILIKE @p1) " +
                "ORDER BY t1.\"Id\" ASC LIMIT @take OFFSET @skip;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void DemoProductReport_GeneratesSqlServerSql_WithoutWhere_WhenNameIsNull()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(name: null, skip: 0, take: 10);

            const string expected =
                "SELECT t1.[Id] AS [ProductId], t1.[Name] AS [ProductName], t1.[Price] AS [Price], t2.[CategoryName] AS [CategoryName], t3.[BrandName] AS [BrandName] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]) LEFT JOIN [Brand] t3 ON (t1.[BrandId] = t3.[Id]) " +
                "ORDER BY t1.[Id] ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_ComparisonOperators_GenerateSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(
                where: (p, c, b) => p.Price > 100m && p.Id <= 10 && p.Id != 5,
                skip: 0,
                take: 10);

            const string expected =
                "SELECT t1.[Id] AS [ProductId], t1.[Name] AS [ProductName], t1.[Price] AS [Price], t2.[CategoryName] AS [CategoryName], t3.[BrandName] AS [BrandName] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]) LEFT JOIN [Brand] t3 ON (t1.[BrandId] = t3.[Id]) " +
                "WHERE (((t1.[Price] > @p1) AND (t1.[Id] <= @p2)) AND (t1.[Id] <> @p3)) " +
                "ORDER BY t1.[Id] ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "p2", "p3", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_NullChecks_GenerateSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(
                where: (p, c, b) => p.ModifiedBy == null || p.ModifiedBy != null,
                skip: 0,
                take: 10);

            const string expected =
                "SELECT t1.[Id] AS [ProductId], t1.[Name] AS [ProductName], t1.[Price] AS [Price], t2.[CategoryName] AS [CategoryName], t3.[BrandName] AS [BrandName] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]) LEFT JOIN [Brand] t3 ON (t1.[BrandId] = t3.[Id]) " +
                "WHERE ((t1.[ModifiedBy] IS NULL) OR (t1.[ModifiedBy] IS NOT NULL)) " +
                "ORDER BY t1.[Id] ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_ComparisonOperators_GeneratePostgreSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(
                where: (p, c, b) => p.Price > 100m && p.Id <= 10 && p.Id != 5,
                skip: 0,
                take: 10);

            const string expected =
                "SELECT t1.\"Id\" AS \"ProductId\", t1.\"Name\" AS \"ProductName\", t1.\"Price\" AS \"Price\", t2.\"CategoryName\" AS \"CategoryName\", t3.\"BrandName\" AS \"BrandName\" " +
                "FROM \"DemoProduct\" t1 JOIN \"Category\" t2 ON (t1.\"CategoryId\" = t2.\"Id\") LEFT JOIN \"Brand\" t3 ON (t1.\"BrandId\" = t3.\"Id\") " +
                "WHERE (((t1.\"Price\" > @p1) AND (t1.\"Id\" <= @p2)) AND (t1.\"Id\" <> @p3)) " +
                "ORDER BY t1.\"Id\" ASC LIMIT @take OFFSET @skip;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "p2", "p3", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_NullChecks_GeneratePostgreSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSql(
                where: (p, c, b) => p.ModifiedBy == null || p.ModifiedBy != null,
                skip: 0,
                take: 10);

            const string expected =
                "SELECT t1.\"Id\" AS \"ProductId\", t1.\"Name\" AS \"ProductName\", t1.\"Price\" AS \"Price\", t2.\"CategoryName\" AS \"CategoryName\", t3.\"BrandName\" AS \"BrandName\" " +
                "FROM \"DemoProduct\" t1 JOIN \"Category\" t2 ON (t1.\"CategoryId\" = t2.\"Id\") LEFT JOIN \"Brand\" t3 ON (t1.\"BrandId\" = t3.\"Id\") " +
                "WHERE ((t1.\"ModifiedBy\" IS NULL) OR (t1.\"ModifiedBy\" IS NOT NULL)) " +
                "ORDER BY t1.\"Id\" ASC LIMIT @take OFFSET @skip;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_In_GenerateSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSqlWithIn(ids: new[] { 1, 2, 3 }, skip: 0, take: 10);

            const string expected =
                "SELECT t1.[Id] AS [ProductId], t1.[Name] AS [ProductName], t1.[Price] AS [Price], t2.[CategoryName] AS [CategoryName], t3.[BrandName] AS [BrandName] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]) LEFT JOIN [Brand] t3 ON (t1.[BrandId] = t3.[Id]) " +
                "WHERE (t1.[Id] IN (@p1, @p2, @p3)) " +
                "ORDER BY t1.[Id] ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "p2", "p3", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_In_GeneratePostgreSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductReportSqlWithIn(ids: new[] { 1, 2, 3 }, skip: 0, take: 10);

            const string expected =
                "SELECT t1.\"Id\" AS \"ProductId\", t1.\"Name\" AS \"ProductName\", t1.\"Price\" AS \"Price\", t2.\"CategoryName\" AS \"CategoryName\", t3.\"BrandName\" AS \"BrandName\" " +
                "FROM \"DemoProduct\" t1 JOIN \"Category\" t2 ON (t1.\"CategoryId\" = t2.\"Id\") LEFT JOIN \"Brand\" t3 ON (t1.\"BrandId\" = t3.\"Id\") " +
                "WHERE (t1.\"Id\" IN (@p1, @p2, @p3)) " +
                "ORDER BY t1.\"Id\" ASC LIMIT @take OFFSET @skip;";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1", "p2", "p3", "skip", "take" },
                result.ParameterNames);
        }

        [TestMethod]
        public void Where_In_EmptyList_Throws()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                service.BuildDemoProductReportSqlWithIn_Empty(skip: 0, take: 10));

            StringAssert.Contains(ex.Message, "IN list is empty");
        }

        [TestMethod]
        public void Where_In_TooLargeList_Throws()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                service.BuildDemoProductReportSqlWithIn_TooLarge(skip: 0, take: 10));

            StringAssert.Contains(ex.Message, "IN list is too large");
        }

        [TestMethod]
        public void GroupBy_AggregateExpression_Having_Distinct_GeneratesSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductAggregateSql(minTotal: 100m, distinct: true);

            const string expected =
                "SELECT DISTINCT t1.[CategoryId] AS [CategoryId], SUM((t1.[Price] * t1.[Qty])) AS [Total] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]) " +
                "GROUP BY t1.[CategoryId] " +
                "HAVING (SUM((t1.[Price] * t1.[Qty])) > @p1);";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1" },
                result.ParameterNames);
        }

        [TestMethod]
        public void GroupBy_AggregateExpression_Having_Distinct_GeneratesPostgreSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductAggregateSql(minTotal: 100m, distinct: true);

            const string expected =
                "SELECT DISTINCT t1.\"CategoryId\" AS \"CategoryId\", SUM((t1.\"Price\" * t1.\"Qty\")) AS \"Total\" " +
                "FROM \"DemoProduct\" t1 JOIN \"Category\" t2 ON (t1.\"CategoryId\" = t2.\"Id\") " +
                "GROUP BY t1.\"CategoryId\" " +
                "HAVING (SUM((t1.\"Price\" * t1.\"Qty\")) > @p1);";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);

            CollectionAssert.AreEquivalent(
                new[] { "p1" },
                result.ParameterNames);
        }

        [TestMethod]
        public void ScalarComputedProjection_GeneratesSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductCalcSql();

            const string expected =
                "SELECT (t1.[Price] * t1.[Qty]) AS [AA], t1.[Qty] AS [Qty] " +
                "FROM [DemoProduct] t1 JOIN [Category] t2 ON (t1.[CategoryId] = t2.[Id]);";

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);
            CollectionAssert.AreEquivalent(Array.Empty<string>(), result.ParameterNames);
        }

        [TestMethod]
        public void ScalarComputedProjection_GeneratesPostgreSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductCalcSql();

            const string expected = $@"SELECT (t1.""Price"" * t1.""Qty"") AS ""AA"", t1.""Qty"" AS ""Qty"" FROM ""DemoProduct"" t1 JOIN ""Category"" t2 ON (t1.""CategoryId"" = t2.""Id"");";
        

            SqlAssertHelpers.AreSqlEqual(expected, result.Sql);
            CollectionAssert.AreEquivalent(Array.Empty<string>(), result.ParameterNames);
        }

        [TestMethod]
        public async Task WhereBuilder_AsyncCapturedLocalInt_GeneratesSqlServerSql()
        {
            var result = await BuildWhereAsync(5);

            const string expected = "(DemoProduct.[CategoryId] = @1)";
            Assert.AreEqual(expected, result.Sql);
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual(5, result.Parameters[0].Value);
        }

        [TestMethod]
        public async Task WhereBuilder_AsyncCapturedLocalString_GeneratesSqlServerSql()
        {
            var result = await BuildNameContainsWhereAsync("abc");

            const string expected = "(DemoProduct.[Name] LIKE @1)";
            Assert.AreEqual(expected, result.Sql);
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("%abc%", result.Parameters[0].Value);
        }

        [TestMethod]
        public async Task WhereBuilder_AsyncCapturedList_GeneratesSqlServerSql()
        {
            var result = await BuildInWhereAsync(new[] { 1, 2, 3 });

            const string expected = "(DemoProduct.[Id] IN (@1,@2,@3))";
            Assert.AreEqual(expected, result.Sql);
            Assert.AreEqual(3, result.Parameters.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, result.Parameters.Select(p => p.Value).ToArray());
        }

        [TestMethod]
        public async Task WhereBuilder_CapturedIntArrayIn_GeneratesSqlServerSql()
        {
            // C# 14/.NET 10 compiles array .Contains() to a span-based call; this intent is the
            // regression test for WhereBuilder.Evaluate handling the op_Implicit span wrapper.
            var result = await BuildInWhereArrayAsync(new[] { 1, 2, 3 });

            const string expected = "(DemoProduct.[Id] IN (@1,@2,@3))";
            Assert.AreEqual(expected, result.Sql);
            Assert.AreEqual(3, result.Parameters.Count);
        }

        [TestMethod]
        public async Task WhereBuilder_CapturedStringArrayIn_GeneratesSqlServerSql()
        {
            // Mirrors the reported backend failure: ids.Contains(x.Property) with string[].
            var result = await BuildInWhereStringArrayAsync(new[] { "A", "B", "C" });

            const string expected = "(DemoProduct.[Name] IN (@1,@2,@3))";
            Assert.AreEqual(expected, result.Sql);
            Assert.AreEqual(3, result.Parameters.Count);
            CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, result.Parameters.Select(p => p.Value).ToArray());
        }

        [TestMethod]
        public async Task DemoProductReport_AsyncCapturedCategory_GeneratesSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = await service.BuildDemoProductReportSqlAsync_CapturedCategory(42, skip: 0, take: 10);

            StringAssert.Contains(result.Sql, "t1.[CategoryId] = @p1");
            CollectionAssert.Contains(result.ParameterNames, "p1");
        }

        [TestMethod]
        public async Task DemoProductReport_AsyncCapturedName_GeneratesSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = await service.BuildDemoProductReportSqlAsync_CapturedName("abc", skip: 0, take: 10);

            StringAssert.Contains(result.Sql, "t1.[Name] LIKE @p1");
            CollectionAssert.Contains(result.ParameterNames, "p1");
        }

        [TestMethod]
        public async Task DemoProductReport_AsyncCapturedIn_GeneratesSqlServerSql()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = await service.BuildDemoProductReportSqlAsync_CapturedIn(new[] { 1, 2, 3 }, skip: 0, take: 10);

            StringAssert.Contains(result.Sql, "t1.[Id] IN (@p1");
            CollectionAssert.Contains(result.ParameterNames, "p1");
            CollectionAssert.Contains(result.ParameterNames, "p2");
            CollectionAssert.Contains(result.ParameterNames, "p3");
        }

        [TestMethod]
        public void BuildQuery_ForUpdate_SqlServer_AppendsTableHint()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductQuerySqlWithLock(42, QueryLockType.ForUpdate, DBTypes.SQLSERVER);

            StringAssert.Contains(result.Sql, "[DemoProduct] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)");
            Assert.IsFalse(result.Sql.Contains("FOR UPDATE"));
        }

        [TestMethod]
        public void BuildQuery_ForUpdate_PostgreSql_AppendsForUpdate()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductQuerySqlWithLock(42, QueryLockType.ForUpdate, DBTypes.POSTGRESQL);

            StringAssert.Contains(result.Sql, "FOR UPDATE");
            Assert.IsFalse(result.Sql.Contains("WITH ("));
        }

        [TestMethod]
        public void BuildQuery_ForUpdate_MySql_AppendsForUpdate()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeMySqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductQuerySqlWithLock(42, QueryLockType.ForUpdate, DBTypes.MYSQL);

            StringAssert.Contains(result.Sql, "FOR UPDATE");
        }

        [TestMethod]
        public void BuildQueryTop_ForUpdate_PostgreSql_AppendsForUpdateAfterLimit()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductQueryTopSqlWithLock(42, QueryLockType.ForUpdate, DBTypes.POSTGRESQL);

            StringAssert.Contains(result.Sql, "limit 1 FOR UPDATE");
        }

        [TestMethod]
        public void PagedFilterOptions_ForUpdate_SqlServer_AppendsTableHint()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductPagedQuerySqlWithLock(QueryLockType.ForUpdate, DBTypes.SQLSERVER);

            StringAssert.Contains(result.Sql, "[DemoProduct] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)");
        }

        [TestMethod]
        public void PagedFilterOptions_ForUpdate_PostgreSql_AppendsForUpdateBeforeLimit()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductPagedQuerySqlWithLock(QueryLockType.ForUpdate, DBTypes.POSTGRESQL);

            StringAssert.Contains(result.Sql, "FOR UPDATE");
            StringAssert.Contains(result.Sql, "LIMIT 10");
        }

        [TestMethod]
        public void JoinQuery_ForUpdate_SqlServer_AppendsTableHint()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqlServerConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductJoinQuerySqlWithLock(QueryLockType.ForUpdate, DBTypes.SQLSERVER);

            StringAssert.Contains(result.Sql, "[DemoProduct] t1 WITH (UPDLOCK, ROWLOCK, HOLDLOCK)");
        }

        [TestMethod]
        public void JoinQuery_ForUpdate_PostgreSql_AppendsForUpdateBeforePaging()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeNpgsqlConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            var result = service.BuildDemoProductJoinQuerySqlWithLock(QueryLockType.ForUpdate, DBTypes.POSTGRESQL);

            StringAssert.Contains(result.Sql, "FOR UPDATE OF t1");
            StringAssert.Contains(result.Sql, "LIMIT @take");
        }

        [TestMethod]
        public void BuildQuery_ForUpdate_Sqlite_ThrowsNotSupported()
        {
            var db = new FakeDbConnectionAccessor(() => new FakeSqliteConnection());
            var service = new SqlGenerationTestService(db, new FixedPrefixProvider(prefix: ""));

            Assert.ThrowsException<NotSupportedException>(() =>
                service.BuildDemoProductQuerySqlWithLock(42, QueryLockType.ForUpdate, DBTypes.SQLITE));
        }

        private static async Task<WherePart> BuildWhereAsync(int categoryId)
        {
            await Task.Yield();
            Expression<Func<DemoProduct, bool>> predicate = p => p.CategoryId == categoryId;
            return predicate.ToSql("DemoProduct", SqlBuilderFactory.DBTypes.SQLSERVER);
        }

        private static async Task<WherePart> BuildNameContainsWhereAsync(string name)
        {
            await Task.Yield();
            Expression<Func<DemoProduct, bool>> predicate = p => p.Name.Contains(name);
            return predicate.ToSql("DemoProduct", SqlBuilderFactory.DBTypes.SQLSERVER);
        }

        private static async Task<WherePart> BuildInWhereAsync(IReadOnlyCollection<int> ids)
        {
            await Task.Yield();
            Expression<Func<DemoProduct, bool>> predicate = p => ids.Contains(p.Id);
            return predicate.ToSql("DemoProduct", SqlBuilderFactory.DBTypes.SQLSERVER);
        }

        private static async Task<WherePart> BuildInWhereArrayAsync(int[] ids)
        {
            await Task.Yield();
            Expression<Func<DemoProduct, bool>> predicate = p => ids.Contains(p.Id);
            return predicate.ToSql("DemoProduct", SqlBuilderFactory.DBTypes.SQLSERVER);
        }

        private static async Task<WherePart> BuildInWhereStringArrayAsync(string[] values)
        {
            await Task.Yield();
            Expression<Func<DemoProduct, bool>> predicate = p => values.Contains(p.Name);
            return predicate.ToSql("DemoProduct", SqlBuilderFactory.DBTypes.SQLSERVER);
        }
    }
}
