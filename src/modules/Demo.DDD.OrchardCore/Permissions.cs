using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Demo.DDD.OrchardCore
{
    public sealed class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageDemoOutbox = new Permission("ManageDemoOutbox", "Manage demo outbox test page");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult<IEnumerable<Permission>>(new[] { ManageDemoOutbox });
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageDemoOutbox }
                }
            };
        }
    }
}
