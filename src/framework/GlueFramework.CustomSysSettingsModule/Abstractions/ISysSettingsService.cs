using GlueFramework.CustomSysSettingsModule.Dtos;

namespace GlueFramework.CustomSysSettingsModule.Abstractions
{
    public  interface ISysSettingsService
    {
        Task<IEnumerable<SiteSettingDto>> GetAllSettings();
        Task<SiteSettingDto> GetSysSettingByKey(string key);
        Task<IEnumerable<SiteSettingDto>> GetByGroup(string group);
        Task<string> GetValue(string key);
        Task<string> GetValue(string key, string group);
        Task<SiteSettingDto?> GetSetting(string key, string group);
        //Task DeleteSysSettingByKey(string key);
        //Task<SiteSettingDto> SaveSysSettingByKey(SiteSettingDto sysSetting);
        //Task BatchSaveSettings(SiteSettingDto[] sysSettings);
        Task DeleteSettingsByGroupName(string groupName);
        Task BatchSaveSettingsByGroup(SiteSettingDto[] sysSettings);
        Task Update(SiteSettingDto dto);
    }
}
