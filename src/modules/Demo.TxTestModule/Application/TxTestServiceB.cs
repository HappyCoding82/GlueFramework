using Demo.TxTestModule.Abstractions;
using Demo.TxTestModule.Domain;
using Demo.TxTestModule.Infrastructure;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;

namespace Demo.TxTestModule.Application;

public sealed class TxTestServiceB : ServiceBase, ITxTestServiceB
{
    private readonly IDALFactory _dalFactory;

    public TxTestServiceB(IDbConnectionAccessor accessor, IDataTablePrefixProvider prefixProvider, IDALFactory dalFactory)
        : base(accessor, prefixProvider)
    {
        _dalFactory = dalFactory;
    }

    public async Task WriteOneAsync(string name)
    {
        using var dbSession = OpenDbSessionScope();
        var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
        await repo.InsertAsync(new TxTestRecord { Name = name, CreatedUtc = DateTime.UtcNow });
    }

    [Transactional]
    public async Task WriteOneTransactionalAsync(string name)
    {
        using var dbSession = OpenDbSessionScope();
        var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
        await repo.InsertAsync(new TxTestRecord { Name = name, CreatedUtc = DateTime.UtcNow });
    }
}
