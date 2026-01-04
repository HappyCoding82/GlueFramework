using GlueFramework.Core.Abstractions;
using CustomSiteSettingsModule.DataModels;

namespace CustomSiteSettingsModule.DALInterfaces
{
    public interface ISiteSettingsDAL:IDALBase
    {
        Task<IEnumerable<CustomSiteSettings>> GetAllAsync();
        Task<CustomSiteSettings> CreateAsync(CustomSiteSettings model);
        Task<CustomSiteSettings> UpdateAsync(CustomSiteSettings model);
        Task DeleteAsync(CustomSiteSettings model);
    }
}
