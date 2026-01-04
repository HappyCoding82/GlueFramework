using GlueFramework.Core.Abstractions;
using GlueFramework.Core.DataLayer;
using CustomSiteSettingsModule.DALInterfaces;
using CustomSiteSettingsModule.DataModels;

namespace CustomSiteSettingsModule.DataLayers
{
    public class SiteSettingsDAL : DALBase, ISiteSettingsDAL
    {
        public SiteSettingsDAL(IDbConnectionAccessor dbConnectionAccessor, IDataTablePrefixProvider tablePrefixProvider) : base(dbConnectionAccessor, tablePrefixProvider)
        {
        }

        public async Task<CustomSiteSettings> CreateAsync(CustomSiteSettings model)
        {
            var repository = GetRepository<CustomSiteSettings>();
            return await repository.InsertAndReturnAsync(model);
        }

        public async Task DeleteAsync(CustomSiteSettings model)
        {
            var repository = GetRepository<CustomSiteSettings>();
            await repository.DeleteAsync(model);
        }

        public async Task<IEnumerable<CustomSiteSettings>> GetAllAsync()
        {
            var repository = GetRepository<CustomSiteSettings>();
            return await repository.GetAllAsync();
        }

        public async Task<CustomSiteSettings> UpdateAsync(CustomSiteSettings model)
        {
            var repository = GetRepository<CustomSiteSettings>();
            return await repository.UpdateAndReturnAsync(model);
        }
    }
}
