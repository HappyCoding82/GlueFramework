using Microsoft.Extensions.Logging;

namespace GlueFramework.Core.Diagnostics
{
    public static class ProfilingLoggerExtensions
    {
        public static ILogger CreateSlowSqlLogger<T>(this ILogger<T> logger)
        {
            return logger;
        }
    }
}
