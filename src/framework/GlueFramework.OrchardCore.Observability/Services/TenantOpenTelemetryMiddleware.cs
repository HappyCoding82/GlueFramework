using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GlueFramework.OrchardCore.Observability.Services
{
    public sealed class TenantOpenTelemetryMiddleware : IMiddleware
    {
        private readonly TenantOpenTelemetryManager _manager;

        public TenantOpenTelemetryMiddleware(TenantOpenTelemetryManager manager)
        {
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await _manager.EnsureStartedAsync(context.RequestAborted);
            await next(context);
        }
    }
}
