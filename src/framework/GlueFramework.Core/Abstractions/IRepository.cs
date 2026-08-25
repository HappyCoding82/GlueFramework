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
        Task<Model> GetSingleOrDefaultByKeyAsync(Model data, QueryLockType lockType);

        Task DeleteAsync(Expression<Func<Model, bool>> exp);
        Task<Model> FirstOrDefaultAsync(Expression<Func<Model, bool>> expression);
        Task<Model> FirstOrDefaultAsync(Expression<Func<Model, bool>> expression, QueryLockType lockType);
        Task<IEnumerable<Model>> QueryTopAsync(Expression<Func<Model, bool>> exp, int number);
        Task<IEnumerable<Model>> QueryTopAsync(Expression<Func<Model, bool>> exp, int number, QueryLockType lockType);

        Task<IEnumerable<Model>> QueryAsync(Expression<Func<Model, bool>> expression);
        Task<IEnumerable<Model>> QueryAsync(Expression<Func<Model, bool>> expression, QueryLockType lockType);
        Task<PagerResult<Model>> PagerSearchAsync(PagedFilterOptions<Model> opts);

        Task<Model> GetByKeyAsync(Model data);
        Task<Model> GetByKeyAsync(Model data, QueryLockType lockType);

        Task<IEnumerable<Model>> GetAllAsync();
        Task<IEnumerable<Model>> GetAllAsync(QueryLockType lockType);

        Task<IEnumerable<Model>> GetTopRecordsAsync(int number);
        Task<IEnumerable<Model>> GetTopRecordsAsync(int number, QueryLockType lockType);
    }
}
