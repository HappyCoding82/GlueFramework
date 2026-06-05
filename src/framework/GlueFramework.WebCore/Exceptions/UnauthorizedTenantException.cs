using System;

namespace GlueFramework.WebCore.Exceptions
{
    public class UnauthorizedTenantException : UnauthorizedAccessException
    {
        public UnauthorizedTenantException() : base() { }
        public UnauthorizedTenantException(string message) : base(message) { }

        public string TenantId { get; set; }

        public string UserId { get; set; }

        public string Email { get; set; }
    }
}
