using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GlueFramework.AuditLogModule
{
    public sealed class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageAuditLogs = new Permission("ManageAuditLogs", "Manage audit logs");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult<IEnumerable<Permission>>(new[] { ManageAuditLogs });
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageAuditLogs }
                }
            };
        }
    }
}
