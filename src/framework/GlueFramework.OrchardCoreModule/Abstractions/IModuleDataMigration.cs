namespace GlueFramework.OrchardCoreModule.Abstractions
{
    [Obsolete]
    public interface IModuleDataMigration<T>
    {
        Task ExcuteSqlFile(YesSql.ISession session);
    }
}
