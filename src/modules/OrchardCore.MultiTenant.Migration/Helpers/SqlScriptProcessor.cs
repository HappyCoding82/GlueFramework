using System.Text.RegularExpressions;

namespace OrchardCore.MultiTenant.Migration.Helpers
{
    public static class SqlScriptProcessor
    {
        public static IEnumerable<string> SplitSql(string sql, string provider)
        {
            if (string.IsNullOrWhiteSpace(sql)) yield break;

            bool isPg = provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);

            var batches = isPg
                ? new string[] { sql }
                : Regex.Split(sql, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    yield return trimmed;
            }
        }
    }

}
