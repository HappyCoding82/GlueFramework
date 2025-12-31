using GlueFramework.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GlueFramework.Core.DataLayer
{
    public class DALFactory :IDALFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DALFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public DAL CreateDAL<DAL>() where DAL : IDALBase
        {
            var dal = _serviceProvider.GetService<DAL>();
            return dal;
        }
    }
}
