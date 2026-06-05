using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using GlueFramework.CustomSysSettingsModule.Abstractions;
using GlueFramework.CustomSysSettingsModule.Dtos;
using OrchardCore.DisplayManagement.Notify;
using GlueFramework.CustomSysSettingsModule.Security;
using Microsoft.AspNetCore.Authorization;
using GlueFramework.CustomSysSettingsModule.Abstrations;

namespace GlueFramework.CustomSysSettingsModule.Controllers
{
    [Admin]
    [ApiExplorerSettings(IgnoreApi = true)]
    //[ServiceFilter(typeof(APIExceptionFilterAttribute))]

    public class CustomSiteSettingsController : Controller
    {
        private readonly INotifier _notifier;
        private readonly ISysSettingsService _sysSettingsService;
        private readonly IStringLocalizer T;
        private readonly IKeyVaultService _keyVaultService;

        public CustomSiteSettingsController(INotifier notifier,
            ISysSettingsService sysSettingsService, 
            IKeyVaultService keyVaultService,
            IStringLocalizer<CustomSiteSettingsController> stringLocalizer)
        {
            _notifier = notifier;
            _sysSettingsService = sysSettingsService;
            T = stringLocalizer;
            _keyVaultService = keyVaultService;
        }
        [MVCAuthorizationFilter(CustomSiteSettingsPermissionProvider.READ_CUSTOMSITESETTINGGS)]
        public async Task<ActionResult> Index()
        {
            var settings = await _sysSettingsService.GetAllSettings();
            //_items.Add(new DictionaryItem() { Id = 1, Name = "S1", Value = "V1", Description = "Des1" });
            return View(settings);
        }

        public static readonly string STR_Settings_Saved = "Settings successfully saved";
        public static readonly string STR_Group_Deleted = "Group successfully deleted";

        //[HttpPost]
        //public async Task<IActionResult> SaveSettings(SiteSettingDto[] settings)
        //{
        //    await _sysSettingsService.BatchSaveSettings(settings);
        //    var mess = new LocalizedHtmlString(STR_Settings_Saved, T[STR_Settings_Saved]);
        //    await _notifier.SuccessAsync(mess);
        //    var viewModel = await _sysSettingsService.GetAllSettings();
        //    return View("Index", viewModel);
        //}

        [HttpPost]
        [MVCAuthorizationFilter(CustomSiteSettingsPermissionProvider.MANAGE_CUSTOMSITESETTINGS)]
        public async Task<IActionResult> SaveSettingsByGroup(SiteSettingDto[] settings)
        {
            try
            {
                await _sysSettingsService.BatchSaveSettingsByGroup(settings);
                var mess = new LocalizedHtmlString(STR_Settings_Saved, T[STR_Settings_Saved]);
                await _notifier.SuccessAsync(mess);
                var viewModel = await _sysSettingsService.GetAllSettings();
                return View("Index", viewModel);
            }
            catch(Exception e)
            {
                await _notifier.ErrorAsync(new LocalizedHtmlString(e.Message, T[e.Message]));
                var viewModel = await _sysSettingsService.GetAllSettings();
                return View("Index", viewModel);
            }
        }

        [HttpPost]
        [MVCAuthorizationFilter(CustomSiteSettingsPermissionProvider.MANAGE_CUSTOMSITESETTINGS)]
        public async Task<IActionResult> DeleteGroupByName([FromForm] string name)
        {
            try {
                await _sysSettingsService.DeleteSettingsByGroupName(name);
                var mess = new LocalizedHtmlString(STR_Group_Deleted, T[STR_Group_Deleted]);
                await _notifier.SuccessAsync(mess);
                var viewModel = await _sysSettingsService.GetAllSettings();
                return View("Index", viewModel);
            }
            catch (Exception e)
            {
                await _notifier.ErrorAsync(new LocalizedHtmlString(e.Message, T[e.Message]));
                var viewModel = await _sysSettingsService.GetAllSettings();
                return View("Index", viewModel);
            }
        }

        [HttpPost]
        [Authorize(CustomSiteSettingsPermissionProvider.MANAGE_CUSTOMSITESETTINGS)]
        public async Task<JsonResult> SaveKeyValut([FromForm] string key, [FromForm] string value)
        {
            try
            {
                var path = await _keyVaultService.SaveData(key, value);
                return new JsonResult(new { success = true, path });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(CustomSiteSettingsPermissionProvider.MANAGE_CUSTOMSITESETTINGS)]
        public async Task<JsonResult> GetKeyValut([FromQuery] string key)
        {
            try
            {
                var result = await _keyVaultService.GetData(key);
                return new JsonResult(new { success = true, value = result });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, value = ex.Message });
            }
        }
    }

}
