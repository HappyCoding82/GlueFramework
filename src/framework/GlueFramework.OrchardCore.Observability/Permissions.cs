using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GlueFramework.OrchardCore.Observability
{
    public sealed class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageObservability = new Permission("ManageObservability", "Manage observability settings");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult<IEnumerable<Permission>>(new[] { ManageObservability });
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageObservability }
                }
            };
        }
    }
}
