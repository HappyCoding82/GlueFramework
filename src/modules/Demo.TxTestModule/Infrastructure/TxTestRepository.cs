using Demo.TxTestModule.Abstractions;
using Demo.TxTestModule.Domain;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.DataLayer;
using Demo.TxTestModule.Infrastructure.DbModels;
using System.Linq;

namespace Demo.TxTestModule.Infrastructure;

public sealed class TxTestRepository : DALBase, ITxTestRepository
{
    public TxTestRepository(IDbSession session, IDataTablePrefixProvider tableNamePrefixProvider) : base(session, tableNamePrefixProvider)
    {
    }

    public async Task<int> InsertAsync(TxTestRecord record)
    {
        var repo = GetRepository<TxTestRecordDbModel>();
        var inserted = await repo.InsertAndReturnAsync(new TxTestRecordDbModel
        {
            Name = record.Name,
            CreatedUtc = record.CreatedUtc
        });
        return inserted.Id;
    }

    public async Task<int> CountAsync()
    {
        var repo = GetRepository<TxTestRecordDbModel>();
        var all = await repo.GetAllAsync();
        return all.Count();
    }

    public async Task DeleteAllAsync()
    {
        var repo = GetRepository<TxTestRecordDbModel>();
        var all = (await repo.GetAllAsync()).ToList();
        if (all.Count == 0)
            return;

        await repo.DeleteAsync(all);
    }
}
