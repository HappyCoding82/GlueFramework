using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;
using CustomSiteSettingsModule.Abstractions;
using CustomSiteSettingsModule.DALInterfaces;
using CustomSiteSettingsModule.DataModels;
using CustomSiteSettingsModule.Dtos;

namespace CustomSiteSettingsModule.Services
{
    public class SysSettingsService : ServiceBase, ISysSettingsService
    {
        private ISiteSettingsDAL _siteSettingsDAL;
        private readonly IModuleServiceContext _serviceContext;

        public SysSettingsService(
            ISiteSettingsDAL siteSettingsDAL,
            IModuleServiceContext serviceContext, IDbConnectionAccessor dbConnectionAccessor, IDataTablePrefixProvider tablePrefixProvider) 
            : base(dbConnectionAccessor, tablePrefixProvider)
        {
            _siteSettingsDAL = siteSettingsDAL;
            _serviceContext = serviceContext;
        }

        private ISiteSettingsDAL DAL
        {
            get
            {
                return _siteSettingsDAL;
            }
        }

        public async Task<IEnumerable<SiteSettingDto>> GetAllSettings()
        {
            var dal = DAL;
            return (await dal.GetAllAsync()).OrderBy(x => x.Group).Select(x => ConvertToDto(x));
        }

        public async Task<SiteSettingDto> GetSysSettingByKey(string key)
        {
            var dal = DAL;
            var rs = await dal.GetAllAsync();
            return ConvertToDto(rs.FirstOrDefault(x => x.SKey == key) ?? new CustomSiteSettings());
        }
        public async Task<IEnumerable<SiteSettingDto>> GetByGroup(string group)
        {
            var dal = DAL;
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
            var dal = DAL;
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

        //public async Task<SiteSettingDto> SaveSysSettingByKey(SiteSettingDto sysSetting)
        //{
        //    var dal = DAL;
        //    var rs = await dal.GetAllAsync();
        //    var dbSetting = rs.FirstOrDefault(x => string.Compare(x.SKey, sysSetting.SKey, true) == 0);
        //    if (dbSetting != null)
        //    {
        //        return ConvertToDto(await dal.UpdateAsync(new CustomSiteSettings
        //        {
        //            ID = dbSetting.ID,
        //            Group = dbSetting.Group,
        //            SKey = sysSetting.SKey,
        //            SValue = sysSetting.SValue,
        //            ReadOnly = dbSetting.ReadOnly,
        //            Removable = dbSetting.Removable,
        //            CreatedBy = dbSetting.CreatedBy,
        //            CreatedDate = dbSetting.CreatedDate,
        //            LastModifiedBy = _serviceContext.GetCurrentUserId() ?? "admin",
        //            LastModifiedDate = DateTime.UtcNow
        //        }));
        //    }
        //    else
        //    {
        //        return ConvertToDto(await dal.CreateAsync(new CustomSiteSettings
        //        {
        //            Group = sysSetting.Group,
        //            SKey = sysSetting.SKey,
        //            SValue = sysSetting.SValue,
        //            ReadOnly = sysSetting.ReadOnly,
        //            Removable = sysSetting.Removable,
        //            CreatedBy = _serviceContext.GetCurrentUserId() ?? "admin",
        //            CreatedDate = DateTime.UtcNow,
        //            LastModifiedBy = _serviceContext.GetCurrentUserId() ?? "admin",
        //            LastModifiedDate = DateTime.UtcNow
        //        }));
        //    }
        //}

        //public async Task BatchSaveSettings(SiteSettingDto[] sysSettings)
        //{
        //    var dal = DAL;
        //    var settings = await dal.GetAllAsync();

        //    foreach (var setting in settings)
        //    {
        //        if (!sysSettings.Any(x => x.SKey == setting.SKey))
        //        {
        //            //Removed
        //            await dal.DeleteAsync(setting);
        //        }
        //    }

        //    foreach (var setting in sysSettings)
        //    {
        //        await SaveSysSettingByKey(setting);
        //    }
        //}

        public async Task DeleteSettingsByGroupName(string groupName)
        {
            var dal = DAL;
            var settings = (await dal.GetAllAsync()).Where(x => x.Group == groupName);
            foreach (var setting in settings)
            {
                await dal.DeleteAsync(setting);
            }
        }

        public async Task<SiteSettingDto?> GetSetting(string key, string group)
        {
            var dal = DAL;
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

        public async Task BatchSaveSettingsByGroup(SiteSettingDto[] newSettings)
        {
            if (newSettings.Length > 0)
            {
                var groupName = newSettings[0].Group;
                var dal = DAL;
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
