namespace CustomSiteSettingsModule.Abstrations
{
    public interface IKeyVaultService
    {
        Task<string> SaveData(string key, string value);
        Task<string> GetData(string key);
         string GetKeyVaultFullPath(string key);
    }
}
