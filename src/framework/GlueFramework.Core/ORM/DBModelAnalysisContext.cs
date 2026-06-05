namespace GlueFramework.Core.ORM
{
    public static class DBModelAnalysisContext
    {
        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TableMapping> Mappings =
            new System.Collections.Concurrent.ConcurrentDictionary<string, TableMapping>();
    }
}
