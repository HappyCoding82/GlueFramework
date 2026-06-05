namespace Demo.TxTestModule.Abstractions;

public enum TxTestCase
{
    NoTransactional_SingleService_TwoWrites_Throw = 1,
    Transactional_SingleService_TwoWrites_Throw = 2,
    Transactional_CrossService_TwoWrites_Throw = 3,
    Transactional_Nested_OuterTransactional_InnerTransactional_Throw = 4,
    Transactional_CrossService_BothTransactional_Throw = 5
}
