using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OrchardCore.Security.Permissions;
using Demo.CustomModule.Attributes;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;

namespace Demo.CustomModule.Filters
{
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService _authorizationService;

        public PermissionAuthorizationFilter(
            IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var attributes = context.ActionDescriptor.EndpointMetadata
                .OfType<RequirePermissionAttribute>()
                .ToList();

            if (!attributes.Any())
                return;

            var user = context.HttpContext.User;
            
            // 添加详细调试信息
            Debug.WriteLine("=== 权限验证调试信息 ===");
            Debug.WriteLine($"User.IsAuthenticated: {user.Identity?.IsAuthenticated}");
            Debug.WriteLine($"User.Identity.Name: {user.Identity?.Name}");
            Debug.WriteLine($"User.Identity.AuthenticationType: {user.Identity?.AuthenticationType}");
            Debug.WriteLine($"User.Claims.Count: {user.Claims.Count()}");
            
            // 输出所有Claims
            foreach (var claim in user.Claims)
            {
                Debug.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }
            
            // 输出请求头
            foreach (var header in context.HttpContext.Request.Headers)
            {
                if (header.Key.StartsWith("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"Authorization Header: {header.Value}");
                }
            }
            
            Debug.WriteLine("====================");

            foreach (var attr in attributes)
            {
                var permission = new Permission(attr.PermissionName);
                var authorized = await _authorizationService
                    .AuthorizeAsync(user, permission);

                if (!authorized)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
    }
}