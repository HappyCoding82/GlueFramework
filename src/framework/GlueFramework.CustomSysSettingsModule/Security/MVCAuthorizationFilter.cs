using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CustomSiteSettingsModule.Security
{
    public class MVCAuthorizationFilter : Attribute, IAsyncAuthorizationFilter
    {
        private string _permissionName = null;
        public MVCAuthorizationFilter(string permissionName)
        {
            _permissionName = permissionName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
                var permissionPolicy = CustomSiteSettingsPermissionProvider.GetPermissionByName(_permissionName);
                var rs = await authService.AuthorizeAsync(context.HttpContext.User, permissionPolicy);
                if (!rs)
                {
                    context.Result = new StatusCodeResult(401);
                    return;
                }
            }
            else
            {
                //todo:GODO LOGON
                context.Result = new StatusCodeResult(401);
                return;
            }
        }
    }
}
