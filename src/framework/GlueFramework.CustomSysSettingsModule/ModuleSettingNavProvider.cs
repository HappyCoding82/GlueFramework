using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace CustomSiteSettingsModule
{
    
    public class ModuleSettingNavProvider : INavigationProvider
    {
        private readonly IStringLocalizer<ModuleSettingNavProvider> T;
        private readonly string STR_ControllerName = "CustomSiteSettings";
        private readonly string KEYVAULT_ControllerName = "KeyVault";
        private readonly string STR_CustomSettings = "Custom Site settings";
        public ModuleSettingNavProvider(IStringLocalizer<ModuleSettingNavProvider> localizer)
        {
            T = localizer;
        }

   
        ValueTask INavigationProvider.BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!string.Equals(name, "admin", System.StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.CompletedTask;
            }

            builder
                .Add(T[STR_CustomSettings], "10",
                     menu => menu.Action("Index", STR_ControllerName, new { area = Constants.ModuleAreaPrefix, groupId = STR_ControllerName })
                                        .Permission(CustomSiteSettingsPermissionProvider.PER_READ_CUSTOMSITESETTINGGS)
                                        .LocalNav())
                .Add(T["KeyVault"], "11",
                     menu => menu.Action("Index", KEYVAULT_ControllerName, new { area = Constants.ModuleAreaPrefix, groupId = KEYVAULT_ControllerName })
                                        .Permission(CustomSiteSettingsPermissionProvider.PER_READ_CUSTOMSITESETTINGGS)
                                        .LocalNav());
            return ValueTask.CompletedTask;
        }
    }
}
