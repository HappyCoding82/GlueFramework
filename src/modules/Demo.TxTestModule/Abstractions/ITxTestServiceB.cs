namespace Demo.TxTestModule.Abstractions;

public interface ITxTestServiceB
{
    Task WriteOneAsync(string name);

    Task WriteOneTransactionalAsync(string name);
}
