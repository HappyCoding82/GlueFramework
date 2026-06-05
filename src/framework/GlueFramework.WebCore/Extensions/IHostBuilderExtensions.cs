using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GlueFramework.WebCore.Extensions
{
    public static class IHostBuilderExtensions
    {
        public static IHostBuilder UseAutofac(this IHostBuilder hostBuilder)
        {
            return hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        }
    }
}
