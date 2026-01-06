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

        public DAL CreateDAL<DAL>(IDbSession session, params object[] args) where DAL : IDALBase
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (args == null || args.Length == 0)
                return ActivatorUtilities.CreateInstance<DAL>(_serviceProvider, session);

            var allArgs = new object[args.Length + 1];
            allArgs[0] = session;
            Array.Copy(args, 0, allArgs, 1, args.Length);
            return ActivatorUtilities.CreateInstance<DAL>(_serviceProvider, allArgs);
        }
    }
}
