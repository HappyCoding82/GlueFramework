using GlueFramework.Core.Abstractions;
using GlueFramework.Core.ORM;
using GlueFramework.Core.UOW;
using System.Reflection;

namespace GlueFramework.CoreTests.Sql
{
    [TestClass]
    public sealed class RepositorySqlGenerationTests
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

        private static ISqlBuilder<T> GetSqlBuilder<T>(Repository<T> repository) where T : class
        {
            var f = typeof(Repository<T>).GetField("_sqlBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f);
            var b = f.GetValue(repository) as ISqlBuilder<T>;
            Assert.IsNotNull(b);
            return b;
        }

        [TestMethod]
        public void PartialUpdate_GeneratesSqlServerSql()
        {
            using var conn = new FakeSqlServerConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedPrefixProvider(prefix: ""));
            var sqlBuilder = GetSqlBuilder(repo);

            var key = new DemoProduct { Id = 123 };
            var cmd = sqlBuilder.BuildPartialUpdateSql(
                key,
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            const string expected =
                "UPDATE [DemoProduct] SET [Name] = @Name,[Price] = @Price WHERE  [Id]=@Id;";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price", "Id" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialUpdate_GeneratesPostgreSql_WithSchemaAndPrefix()
        {
            using var conn = new FakeNpgsqlConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedTenantTableSettingsProvider(prefix: "s_", schema: "public"));
            var sqlBuilder = GetSqlBuilder(repo);

            var key = new DemoProduct { Id = 123 };
            var cmd = sqlBuilder.BuildPartialUpdateSql(
                key,
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            const string expected =
                "UPDATE \"public\".\"s_DemoProduct\" SET \"Name\" = @Name,\"Price\" = @Price WHERE  \"Id\"=@Id;";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price", "Id" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialUpdate_GeneratesSqlServerSql_WithSchemaAndPrefix()
        {
            using var conn = new FakeSqlServerConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedTenantTableSettingsProvider(prefix: "s_", schema: "dbo"));
            var sqlBuilder = GetSqlBuilder(repo);

            var key = new DemoProduct { Id = 123 };
            var cmd = sqlBuilder.BuildPartialUpdateSql(
                key,
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            const string expected =
                "UPDATE [dbo].[s_DemoProduct] SET [Name] = @Name,[Price] = @Price WHERE  [Id]=@Id;";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price", "Id" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialUpdate_GeneratesPostgreSql()
        {
            using var conn = new FakeNpgsqlConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedPrefixProvider(prefix: ""));
            var sqlBuilder = GetSqlBuilder(repo);

            var key = new DemoProduct { Id = 123 };
            var cmd = sqlBuilder.BuildPartialUpdateSql(
                key,
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            const string expected =
                "UPDATE \"DemoProduct\" SET \"Name\" = @Name,\"Price\" = @Price WHERE  \"Id\"=@Id;";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price", "Id" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialInsert_GeneratesSqlServerSql()
        {
            using var conn = new FakeSqlServerConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedPrefixProvider(prefix: ""));
            var sqlBuilder = GetSqlBuilder(repo);

            var cmd = sqlBuilder.BuildPartialInsertSql(
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            const string expected =
                "INSERT INTO [DemoProduct] ([Name],[Price]) VALUES (@Name,@Price);";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialInsert_GeneratesPostgreSql()
        {
            using var conn = new FakeNpgsqlConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedPrefixProvider(prefix: ""));
            var sqlBuilder = GetSqlBuilder(repo);

            var cmd = sqlBuilder.BuildPartialInsertSql(
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            const string expected =
                "INSERT INTO \"DemoProduct\" (\"Name\",\"Price\") VALUES (@Name,@Price);";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialInsert_IgnoresAutoGenerateFields()
        {
            using var conn = new FakeSqlServerConnection();
            var repo = new Repository<DemoProductIdentity>(conn, new FixedPrefixProvider(prefix: ""));
            var sqlBuilder = GetSqlBuilder(repo);

            var cmd = sqlBuilder.BuildPartialInsertSql(
                new Dictionary<string, object?>
                {
                    [nameof(DemoProductIdentity.Id)] = 123,
                    [nameof(DemoProductIdentity.Name)] = "abc",
                });

            const string expected =
                "INSERT INTO [DemoProductIdentity] ([Name]) VALUES (@Name);";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name" }, cmd.Value.ParameterNames.ToArray());
        }

        [TestMethod]
        public void PartialInsert_GeneratesMySqlSql_WithPrefix_AndIgnoresSchema()
        {
            using var conn = new FakeMySqlConnection();
            var repo = new Repository<DemoProduct>(conn, new FixedTenantTableSettingsProvider(prefix: "s_", schema: "dbo"));
            var sqlBuilder = GetSqlBuilder(repo);

            var cmd = sqlBuilder.BuildPartialInsertSql(
                new Dictionary<string, object?>
                {
                    [nameof(DemoProduct.Name)] = "abc",
                    [nameof(DemoProduct.Price)] = 12.3m,
                });

            // MySQL uses backtick quoting; schema is emitted as a qualified name.
            const string expected =
                "INSERT INTO `dbo`.`s_DemoProduct` (`Name`,`Price`) VALUES (@Name,@Price);";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
            CollectionAssert.AreEquivalent(new[] { "Name", "Price" }, cmd.Value.ParameterNames.ToArray());
        }
        [DataTable("Card")]
        private sealed class DemoCard
        {
            [DBField("Id", isKeyField: true, autogenerate: true)]
            public int Id { get; set; }

            [DBField("CreatedUtc")]
            public DateTime CreatedUtc { get; set; }
        }


        [TestMethod]
        public void PagerQuery_GeneratesPostgreSql_WithLambdaOrderByDescending()
        {
            using var conn = new FakeNpgsqlConnection();
            var repo = new Repository<DemoCard>(conn, new FixedTenantTableSettingsProvider(prefix: "", schema: "cm"));
            var sqlBuilder = GetSqlBuilder(repo);

            var opts = new PagedFilterOptions<DemoCard>(
                whereClause: x => x.Id >= 0,
                pager: new PagerInfo { PageIndex = 1, PageSize = 20 },
                orderBy: x => x.CreatedUtc,
                desc: true);

            var cmd = sqlBuilder.BuildQuery(opts);

            const string expected =
                "SELECT COUNT(1) FROM \"cm\".\"Card\"  WHERE (\"cm\".\"Card\".\"Id\" >= 0);" +
                "SELECT \"Id\",\"CreatedUtc\" FROM \"cm\".\"Card\"  WHERE (\"cm\".\"Card\".\"Id\" >= 0) ORDER BY \"CreatedUtc\" DESC LIMIT 20 OFFSET 0";

            SqlAssertHelpers.AreSqlEqual(expected, cmd.Key);
        }


    }
}
