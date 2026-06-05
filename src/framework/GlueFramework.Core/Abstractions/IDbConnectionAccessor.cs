using System.Data.Common;

namespace GlueFramework.Core.Abstractions
{
    public interface IDbConnectionAccessor
    {
        DbConnection CreateConnection();
    }
}
