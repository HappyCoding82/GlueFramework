using GlueFramework.Core.ORM;

namespace Demo.TxTestModule.Infrastructure.DbModels;

[DataTable("Demo_TxTestRecord")]
public sealed class TxTestRecordDbModel
{
    [DBField("Id", isKeyField: true, autogenerate: true)]
    public int Id { get; set; }

    [DBField("Name")]
    public string Name { get; set; } = string.Empty;

    [DBField("CreatedUtc")]
    public DateTime CreatedUtc { get; set; }
}
