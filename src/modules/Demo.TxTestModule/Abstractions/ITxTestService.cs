namespace Demo.TxTestModule.Abstractions;

public interface ITxTestService
{
    Task<int> GetCountAsync();
    Task ResetAsync();

    Task NoTransactional_SingleService_TwoWritesAsync(bool shouldSucceed);
    Task Transactional_SingleService_TwoWritesAsync(bool shouldSucceed);
    Task Transactional_CrossService_TwoWritesAsync(bool shouldSucceed);
    Task Transactional_Nested_OuterTransactional_InnerTransactionalAsync(bool shouldSucceed);
    Task Transactional_CrossService_BothTransactionalAsync(bool shouldSucceed);
}
