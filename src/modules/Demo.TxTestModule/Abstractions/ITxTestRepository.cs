using Demo.TxTestModule.Domain;
using GlueFramework.Core.Abstractions;

namespace Demo.TxTestModule.Abstractions;

public interface ITxTestRepository : IDALBase
{
    Task<int> InsertAsync(TxTestRecord record);
    Task<int> CountAsync();
    Task DeleteAllAsync();
}
