namespace GlueFramework.Core.Abstractions.Dtos
{
    public interface IModifyDto<DbModel> where DbModel : class
    {
        DbModel ConvertToDbModel(DbModel existingRecord,string currentUserId);

        DbModel ConvertToDbModelWithKeyOnly();
    }
}
