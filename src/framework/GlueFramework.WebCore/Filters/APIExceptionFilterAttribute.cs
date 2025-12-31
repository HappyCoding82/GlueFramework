using GlueFramework.WebCore.Exceptions;
using GlueFramework.WebCore.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GlueFramework.WebCore.Filters
{
    public class APIExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private ILogger<APIExceptionFilterAttribute> _logger;
        private IWebHostEnvironment _env;

        public APIExceptionFilterAttribute(ILogger<APIExceptionFilterAttribute> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public override Task OnExceptionAsync(ExceptionContext context)
        {
            if (_logger != null)
                _logger.LogError($"{context.HttpContext.GetCurrentUserId()} -- {context.ActionDescriptor.DisplayName} - {context.Exception.Message}");
                _logger.LogError(context.Exception.ToString());
            context.HttpContext.Response.StatusCode = 500;

            if (context.Exception is BusinessException)
            {
                context.Result = new JsonResult(new { IsUnexpectedException = false, Error = context.Exception.Message });
            }
            else if (context.Exception is BusinessAcceptedException)
            {
                context.HttpContext.Response.StatusCode = 202;
                context.Result = new JsonResult(new { IsUnexpectedException = false, Error = context.Exception.Message });
            }
            else if (context.Exception is DataScopeNotPermittedException)
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new JsonResult(new { IsUnexpectedException = false, Error = context.Exception.Message });
            }
            else if (context.Exception is RoleNotPermittedException)
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new JsonResult(new { IsUnexpectedException = false, Error = context.Exception.Message });
            }
            else
                context.Result = new JsonResult(new { IsUnexpectedException = true, Error = context.Exception.Message });

            return base.OnExceptionAsync(context);
        }
    }
}
