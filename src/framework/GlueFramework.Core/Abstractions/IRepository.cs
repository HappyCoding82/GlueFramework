using System.Data.Common;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace GlueFramework.Core.Abstractions
{
    public interface IRepository<Model>
    {
        Task InsertAsync(Model data);

        Task<int> InsertPartialAsync(Action<PatchBuilder<Model>> patch);

        Task InsertAsync(List<Model> models);
        Task<Model> InsertAndReturnAsync(Model data);

        Task<Model> UpdateAndReturnAsync(Model data);

        Task UpdateAsync(Model data);

        Task<int> UpdatePartialAsync(Model keyModel, Action<PatchBuilder<Model>> patch);

        Task<int> UpdateAsync(List<Model> models);

        Task DeleteAsync(Model data);

        Task DeleteAsync(List<Model> models);

        Task<Model> GetSingleOrDefaultByKeyAsync(Model data);

        Task DeleteAsync(Expression<Func<Model, bool>> exp);
        Task<Model> FirstOrDefaultAsync(Expression<Func<Model, bool>> expression);
        Task<IEnumerable<Model>> QueryTopAsync(Expression<Func<Model, bool>> exp, int number);

        Task<IEnumerable<Model>> QueryAsync(Expression<Func<Model, bool>> expression);
        Task<PagerResult<Model>> PagerSearchAsync(FilterOptions<Model> opts);

        Task<Model> GetByKeyAsync(Model data);

        Task<IEnumerable<Model>> GetAllAsync();

        Task<IEnumerable<Model>> GetTopRecordsAsync(int number);
    }
}
