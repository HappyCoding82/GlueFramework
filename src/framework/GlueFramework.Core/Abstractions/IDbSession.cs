using System.Data;

namespace GlueFramework.Core.Abstractions
{
    public interface IDbSession
    {
        IDbConnection Connection { get; }

        IDbTransaction? Transaction { get; }
    }
}
