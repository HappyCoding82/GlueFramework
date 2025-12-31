using GlueFramework.Core.Abstractions;

namespace GlueFramework.Core.Abstractions
{
    public interface IDALFactory 
    {
        DAL CreateDAL<DAL>() where DAL : IDALBase;
    }
}
