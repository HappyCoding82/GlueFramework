namespace GlueFramework.Core.Abstractions
{
    public interface ISqlBuilderPartition
    {
        string GetInsertSql<T>(T model) where T:PartitionModelBase;

        string GetInsertAndReturnSql<T>(T model) where T : PartitionModelBase;

        string GetSelectByKeySql<T>(T model) where T : PartitionModelBase;

        string GetUpdateSql<T>(T model) where T : PartitionModelBase;

        string GetUpdateAndReturnSql<T>(T model) where T : PartitionModelBase;

        string GetDeleteByKey<T>(T model) where T : PartitionModelBase;
    }
}
