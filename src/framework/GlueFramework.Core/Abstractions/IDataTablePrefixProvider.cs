namespace GlueFramework.Core.Abstractions
{
    public interface IDataTablePrefixProvider
    {
        string Prefix { get; }
    }

    public interface ITenantTableSettingsProvider : IDataTablePrefixProvider
    {
        string? Schema { get; }
    }
}
