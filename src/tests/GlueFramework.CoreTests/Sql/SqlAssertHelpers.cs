using System.Text.RegularExpressions;

namespace GlueFramework.CoreTests.Sql
{
    internal static class SqlAssertHelpers
    {
        public static string Normalize(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return string.Empty;

            sql = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            sql = Regex.Replace(sql, "\\s+", " ").Trim();
            return sql;
        }

        public static void AreSqlEqual(string expected, string actual)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(
                Normalize(expected),
                Normalize(actual));
        }
    }
}
