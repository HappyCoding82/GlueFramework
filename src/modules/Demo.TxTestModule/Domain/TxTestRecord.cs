namespace Demo.TxTestModule.Domain;

public sealed class TxTestRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
