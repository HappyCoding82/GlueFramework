using OrchardCore.Security.Permissions;

namespace CustomSiteSettingsModule
{
    public class CustomSiteSettingsPermissionProvider : IPermissionProvider
    {
        public const string READ_CUSTOMSITESETTINGGS = "ReadCustomSiteSettings";
        public const string MANAGE_CUSTOMSITESETTINGS = "ManageCustomSiteSettings";
        public static readonly Permission PER_READ_CUSTOMSITESETTINGGS = new(READ_CUSTOMSITESETTINGGS, "View Custom site settings");
        public static readonly Permission PER_MANAGE_CUSTOMSITESETTINGS = new(MANAGE_CUSTOMSITESETTINGS, "Manage Custom Site Settings");

        //,new List<Permission>() { PER_READ_CUSTOMSITESETTINGGS }

        public static Permission GetPermissionByName(string name)
        {
            switch (name)
            {
                case READ_CUSTOMSITESETTINGGS: return PER_READ_CUSTOMSITESETTINGGS;
                case MANAGE_CUSTOMSITESETTINGS: return PER_MANAGE_CUSTOMSITESETTINGS;
                default:
                    return null;
            }
        }


        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
             // Giving some defaults: which roles should possess which permissions.
             new List<PermissionStereotype>();


        public Task<IEnumerable<Permission>> GetPermissionsAsync() =>
            Task.FromResult(new[]
            {
                PER_READ_CUSTOMSITESETTINGGS,
                PER_MANAGE_CUSTOMSITESETTINGS
            }
            .AsEnumerable());
    }
}
