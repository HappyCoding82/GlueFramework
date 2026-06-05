using GlueFramework.Core.Abstractions;

namespace GlueFramework.Core.Abstractions
{
    public interface IDALFactory 
    {
        DAL CreateDAL<DAL>(IDbSession session, params object[] args) where DAL : IDALBase;
    }
}
