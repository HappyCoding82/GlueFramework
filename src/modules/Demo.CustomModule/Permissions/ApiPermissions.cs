using OrchardCore.Security.Permissions;

namespace Demo.CustomModule.Permissions
{
    public class ApiPermissions : IPermissionProvider
    {
        public const string ViewProductsPermission = "ViewProducts";
        public const string ManageProductsPermission = "ManageProducts";
        public const string ViewTokenTestResourcesPermission = "ViewTokenTestResources";
        public const string ManageTokenTestSettingsPermission = "ManageTokenTestSettings";

        public static readonly Permission ViewProducts = new Permission(ViewProductsPermission, "View Products");

        public static readonly Permission ManageProducts = new Permission(ManageProductsPermission, "Manage Products");

        public static readonly Permission ViewTokenTestResources = new Permission(ViewTokenTestResourcesPermission, "View Token Test Resources");

        public static readonly Permission ManageTokenTestSettings = new Permission(ManageTokenTestSettingsPermission, "Manage Token Test Settings");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
            => Task.FromResult(new[]
            {
                ViewProducts,
                ManageProducts,
                ViewTokenTestResources,
                ManageTokenTestSettings
            }.AsEnumerable());

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            yield return new PermissionStereotype
            {
                Name = "Administrator",
                Permissions = new[]
                {
                    ManageProducts,
                    ManageTokenTestSettings
                }
            };
        }
    }
}