namespace GlueFramework.Core.Abstractions
{
    public interface IPartitionRepository<Model> where Model:PartitionModelBase
    {
        Task InsertAsync(Model data);
        Task<Model> InsertAndReturnAsync(Model data);
        Task<Model> UpdateAndReturnAsync(Model data);
        Task ExecuteCmdAsyn(string updateCmd, Model data);
        Task UpdateAsync(Model data);
        Task DeleteAsync(Model data);
        Task<Model> GetSingleOrDefaultByKeyAsync(Model data);
        Task<Model> GetByKeyAsync(Model data);
    }
}
