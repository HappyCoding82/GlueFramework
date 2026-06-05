using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;
using GlueFramework.CustomSysSettingsModule.Abstractions;
using GlueFramework.CustomSysSettingsModule.DALInterfaces;
using GlueFramework.CustomSysSettingsModule.DataLayers;
using GlueFramework.CustomSysSettingsModule.DataModels;
using GlueFramework.CustomSysSettingsModule.Dtos;

namespace GlueFramework.CustomSysSettingsModule.Services
{
    public class SysSettingsService : ServiceBase, ISysSettingsService
    {
        //private ISiteSettingsDAL _siteSettingsDAL;
        private IDALFactory _dALFactory;
        private readonly IModuleServiceContext _serviceContext;

        public SysSettingsService(
            IDALFactory dalFactory,
            IModuleServiceContext serviceContext, IDbConnectionAccessor dbConnectionAccessor, IDataTablePrefixProvider tablePrefixProvider) 
            : base(dbConnectionAccessor, tablePrefixProvider)
        {
            _dALFactory = dalFactory;
            _serviceContext = serviceContext;
        }

        private ISiteSettingsDAL GetDAL(IDbSession session)
        {

            return _dALFactory.CreateDAL<SiteSettingsDAL>(session);
        }

        public async Task<IEnumerable<SiteSettingDto>> GetAllSettings()
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);
            return (await dal.GetAllAsync()).OrderBy(x => x.Group).Select(x => ConvertToDto(x));
        }

        public async Task<SiteSettingDto> GetSysSettingByKey(string key)
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);
            var rs = await dal.GetAllAsync();
            return ConvertToDto(rs.FirstOrDefault(x => x.SKey == key) ?? new CustomSiteSettings());
        }
        public async Task<IEnumerable<SiteSettingDto>> GetByGroup(string group)
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);
            var settings = (await dal.GetAllAsync()).Where(x => x.Group == group);
            return settings.Select(ConvertToDto);
        }

        public async Task<string> GetValue(string key)
        {
            var dto = await GetSysSettingByKey(key);
            return dto.SValue;
        }

        public async Task<string> GetValue(string key, string group)
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);
            var dto = (await dal.GetAllAsync()).FirstOrDefault(x => x.SKey == key && x.Group == group);
            if (dto == null)
            {
                await dal.CreateAsync(new CustomSiteSettings
                {
                    SKey = key,
                    SValue = "",
                    Group = group
                });
                return "";
            }
            return dto.SValue ?? "";
        }

        public async Task DeleteSettingsByGroupName(string groupName)
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);
            var settings = (await dal.GetAllAsync()).Where(x => x.Group == groupName);
            foreach (var setting in settings)
            {
                await dal.DeleteAsync(setting);
            }
        }

        public async Task<SiteSettingDto?> GetSetting(string key, string group)
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);
            var dto = (await dal.GetAllAsync()).FirstOrDefault(x => x.SKey == key && x.Group == group);
            if (dto == null || dto.ID < 1)
            {
                return null;
            }
            return ConvertToDto(dto);
        }

        public async Task Update(SiteSettingDto dto)
        {
            using var scope = OpenJoinQuerySessionScope();
            var repos = scope.GetRepository<CustomSiteSettings>();
            var item =await repos.GetByKeyAsync(new CustomSiteSettings { ID = dto.Id });
            item.SValue = dto.SValue;
            item.LastModifiedBy = "admin";
            item.LastModifiedDate = DateTime.UtcNow;
            await repos.UpdateAsync(item);
        }

        [Transactional()]
        public async Task BatchSaveSettingsByGroup(SiteSettingDto[] newSettings)
        {
            if (newSettings.Length > 0)
            {
                var groupName = newSettings[0].Group;
                using var scope = OpenDbSessionScope();
                var dal = GetDAL(scope);
                var originalSettings = (await dal.GetAllAsync())
                    .Where(x => x.Group == groupName)
                    .ToList();

                var insertSettings = newSettings
                    .Where(newSetting => newSetting.Id == 0)
                    .ToList();

                var deleteSettings = originalSettings
                    .Where(originalSetting => !newSettings.Any(newSetting => newSetting.Id == originalSetting.ID))
                    .ToList();

                var updateSettings = newSettings
                    .Where(newSetting => originalSettings.Any(originalSetting =>
                        originalSetting.SKey == newSetting.SKey && (originalSetting.SValue != newSetting.SValue || originalSetting.DefaultVisible != newSetting.DefaultVisible)))
                    .Select(newSetting =>
                    {
                        var originalSetting = originalSettings.FirstOrDefault(x => x.SKey == newSetting.SKey);

                        return new CustomSiteSettings
                        {
                            ID = newSetting.Id,
                            Group = newSetting.Group,
                            SKey = newSetting.SKey,
                            SValue = newSetting.SValue,
                            ReadOnly = newSetting.ReadOnly,
                            Removable = newSetting.Removable,
                            DefaultVisible = newSetting.DefaultVisible,
                            CreatedBy = originalSetting?.CreatedBy ?? _serviceContext.GetCurrentUserId() ?? "admin",
                            CreatedDate = originalSetting?.CreatedDate ?? DateTime.UtcNow,
                            LastModifiedBy = _serviceContext.GetCurrentUserId() ?? "admin",
                            LastModifiedDate = DateTime.UtcNow
                        };
                    })
                    .ToList();
             
                foreach (var item in insertSettings)
                {
                    await dal.CreateAsync(new CustomSiteSettings
                    {
                        Group = item.Group,
                        SKey = item.SKey,
                        SValue = item.SValue,
                        ReadOnly = item.ReadOnly,
                        Removable = item.Removable,
                        DefaultVisible = item.DefaultVisible,
                        CreatedBy = _serviceContext.GetCurrentUserId() ?? "admin",
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedBy = _serviceContext.GetCurrentUserId() ?? "admin",
                        LastModifiedDate = DateTime.UtcNow
                    });
                }
                
                foreach (var item in deleteSettings)
                {
                    await dal.DeleteAsync(new CustomSiteSettings { ID = item.ID });
                }
                foreach (var item in updateSettings)
                {
                    await dal.UpdateAsync(item);
                }
            }
        }

        private SiteSettingDto ConvertToDto(CustomSiteSettings model)
        {
            return new SiteSettingDto
            {
                Id = model.ID,
                Group = model.Group,
                SKey = model.SKey,
                SValue = model.SValue ?? "",
                ReadOnly = model.ReadOnly,
                DefaultVisible = model.DefaultVisible,
                Removable = model.Removable,
                LastModifiedDate = model.LastModifiedDate,
            };
        }
    }

}
