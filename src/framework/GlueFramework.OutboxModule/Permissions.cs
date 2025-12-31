using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GlueFramework.OutboxModule
{
    public sealed class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageOutbox = new Permission("ManageOutbox", "Manage outbox");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult<IEnumerable<Permission>>(new[] { ManageOutbox });
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageOutbox }
                }
            };
        }
    }
}
