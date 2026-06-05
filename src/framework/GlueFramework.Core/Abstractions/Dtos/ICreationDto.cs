namespace GlueFramework.Core.Abstractions.Dtos
{
    public interface ICreationDto<DbModel> where DbModel : class
    {
        DbModel ConvertToDbModel(string currentUserId);
    }
}
