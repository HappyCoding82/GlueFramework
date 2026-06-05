using Demo.TxTestModule.Abstractions;
using Demo.TxTestModule.Domain;
using Demo.TxTestModule.Infrastructure;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;

namespace Demo.TxTestModule.Application;

public sealed class TxTestService : ServiceBase, ITxTestService
{
    private readonly IDALFactory _dalFactory;
    private readonly ITxTestServiceB _serviceB;

    public TxTestService(
        IDbConnectionAccessor accessor,
        IDataTablePrefixProvider prefixProvider,
        IDALFactory dalFactory,
        ITxTestServiceB serviceB)
        : base(accessor, prefixProvider)
    {
        _dalFactory = dalFactory;
        _serviceB = serviceB;
    }

    public async Task<int> GetCountAsync()
    {
        using var dbSession = OpenDbSessionScope();
        var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
        return await repo.CountAsync();
    }

    public async Task ResetAsync()
    {
        using var dbSession = OpenDbSessionScope();
        var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
        await repo.DeleteAllAsync();
    }

    public async Task NoTransactional_SingleService_TwoWritesAsync(bool shouldSucceed)
    {
        using (var dbSession = OpenDbSessionScope())
        {
            var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
            await repo.InsertAsync(new TxTestRecord { Name = "A1", CreatedUtc = DateTime.UtcNow });
        }

        using (var dbSession = OpenDbSessionScope())
        {
            var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
            await repo.InsertAsync(new TxTestRecord { Name = "A2", CreatedUtc = DateTime.UtcNow });
        }

        if (!shouldSucceed)
            throw new InvalidOperationException("fail");
    }

    [Transactional]
    public async Task Transactional_SingleService_TwoWritesAsync(bool shouldSucceed)
    {
        using (var dbSession = OpenDbSessionScope())
        {
            var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
            await repo.InsertAsync(new TxTestRecord { Name = "T1", CreatedUtc = DateTime.UtcNow });
        }

        using (var dbSession = OpenDbSessionScope())
        {
            var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
            await repo.InsertAsync(new TxTestRecord { Name = "T2", CreatedUtc = DateTime.UtcNow });
        }

        if (!shouldSucceed)
            throw new InvalidOperationException("fail");
    }

    [Transactional]
    public async Task Transactional_CrossService_TwoWritesAsync(bool shouldSucceed)
    {
        using (var dbSession = OpenDbSessionScope())
        {
            var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
            await repo.InsertAsync(new TxTestRecord { Name = "X1", CreatedUtc = DateTime.UtcNow });
        }

        await _serviceB.WriteOneAsync("X2");

        if (!shouldSucceed)
            throw new InvalidOperationException("fail");
    }

    [Transactional]
    public async Task Transactional_Nested_OuterTransactional_InnerTransactionalAsync(bool shouldSucceed)
    {
        await _serviceB.WriteOneTransactionalAsync("N1");

        if (!shouldSucceed)
            throw new InvalidOperationException("fail");
    }

    [Transactional]
    public async Task Transactional_CrossService_BothTransactionalAsync(bool shouldSucceed)
    {
        using (var dbSession = OpenDbSessionScope())
        {
            var repo = _dalFactory.CreateDAL<TxTestRepository>(dbSession);
            await repo.InsertAsync(new TxTestRecord { Name = "XB1", CreatedUtc = DateTime.UtcNow });
        }

        await _serviceB.WriteOneTransactionalAsync("XB2");

        if (!shouldSucceed)
            throw new InvalidOperationException("fail");
    }
}
