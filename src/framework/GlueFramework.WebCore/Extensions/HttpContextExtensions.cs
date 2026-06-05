using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GlueFramework.WebCore.Extensions
{
    public static class HttpContextExtensions
    {
        public static string? GetCurrentUserId(this HttpContext context)
        {
            if (context.User == null || context.User.Identity == null)
                return "";
            return context.User.Identity.Name;
        }

    }
}
