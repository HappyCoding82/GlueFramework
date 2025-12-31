using Castle.Components.DictionaryAdapter;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Dtos;

namespace GlueFramework.Core.Services
{
    public abstract class CRUDServiceBase<CreationDto, ModifyDto, ReturnDto,DataModel> :ServiceBase//,ICRUDServiceBase<CreationDto, ModifyDto, ReturnDto>
        where DataModel : class
        where ReturnDto : class
        where ModifyDto : IModifyDto<DataModel>
        where CreationDto : ICreationDto<DataModel>
    {

        public CRUDServiceBase(IDbConnectionAccessor dbConnectionAccessor, IDataTablePrefixProvider dataTablePrefixProvider):
            base(dbConnectionAccessor, dataTablePrefixProvider)
        {
        }

        protected virtual ReturnDto ConvertDbModelToReturnDto(DataModel dbModel)
        { 
            throw new NotImplementedException();
        }

        protected virtual string GetCurrentUserId()
        { 
            throw new NotImplementedException();
        }

        public async virtual Task<ReturnDto?> Create(CreationDto dto)
        {
            using var s = OpenJoinQuerySessionScope();
            var dbModel = await s.GetRepository<DataModel>()
                .InsertAndReturnAsync(dto.ConvertToDbModel(GetCurrentUserId()));
            return ConvertDbModelToReturnDto(dbModel);
        }

        protected async virtual Task Delete(DataModel model)
        {
            using var s = OpenJoinQuerySessionScope();
            await s.GetRepository<DataModel>()
             .DeleteAsync(model);
        }

        public async virtual Task<IEnumerable<ReturnDto>> GetAll()
        {
            using var s = OpenJoinQuerySessionScope();
            var dbModels = await s.GetRepository<DataModel>()
              .GetAllAsync();
            return dbModels.Select(x => ConvertDbModelToReturnDto(x));
        }

        protected async virtual Task<ReturnDto?> GetByKey(DataModel model)
        {
            using var s = OpenJoinQuerySessionScope();
            var dbModel = await s.GetRepository<DataModel>()
               .GetByKeyAsync(model!);
            return ConvertDbModelToReturnDto(dbModel);
        }

        protected async Task<DataModel> GetDataModelByKey(DataModel model)
        {
            using var s = OpenJoinQuerySessionScope();
            var dbModel = await s.GetRepository<DataModel>()
               .GetByKeyAsync(model!);
            return dbModel;
        }

        protected async virtual Task<ReturnDto> Update(DataModel existingRecord, ModifyDto dto)
        {
            using var s = OpenJoinQuerySessionScope();
            var dbModel = await s.GetRepository<DataModel>()
                .UpdateAndReturnAsync(dto.ConvertToDbModel(existingRecord,GetCurrentUserId()));
            return ConvertDbModelToReturnDto(dbModel);
        }

        public async virtual Task<ReturnDto> Update(ModifyDto dto)
        { 
            var existingRecord = await GetDataModelByKey(dto.ConvertToDbModelWithKeyOnly());
            return await Update(existingRecord, dto);
        }

        public async virtual Task DeleteByKey(ModifyDto dto)
        {
             await Delete(dto.ConvertToDbModelWithKeyOnly());
        }
    }
}
