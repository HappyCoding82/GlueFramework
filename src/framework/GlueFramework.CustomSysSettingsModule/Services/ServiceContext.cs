using Microsoft.AspNetCore.Http;
using CustomSiteSettingsModule.Abstractions;
using System;

namespace CustomSiteSettingsModule.Services
{
    public class ServiceContext : IModuleServiceContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ServiceContext(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext == null ? "" : 
                (_httpContextAccessor.HttpContext.User.Identity == null ? "" :
                _httpContextAccessor.HttpContext.User.Identity.Name);
        }
    }
}
