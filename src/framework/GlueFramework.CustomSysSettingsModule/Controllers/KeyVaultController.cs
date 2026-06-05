using GlueFramework.CustomSysSettingsModule.Abstrations;
using GlueFramework.CustomSysSettingsModule.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;

namespace GlueFramework.CustomSysSettingsModule.Controllers
{
    [Admin]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class KeyVaultController : Controller
    {
        private readonly IKeyVaultService _keyVaultService;

        public KeyVaultController(IKeyVaultService keyVaultService)
        {
            _keyVaultService = keyVaultService;
        }

        [MVCAuthorizationFilter(CustomSiteSettingsPermissionProvider.READ_CUSTOMSITESETTINGGS)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [MVCAuthorizationFilter(CustomSiteSettingsPermissionProvider.READ_CUSTOMSITESETTINGGS)]
        public async Task<IActionResult> Save([FromForm] string key, [FromForm] string value)
        {
            string? message;
            try
            {
                var path = await _keyVaultService.SaveData(key, value);
                message = $"已保存 Key '{key}' 到 KeyVault，文件路径：{path}";
            }
            catch (Exception ex)
            {
                message = $"保存失败: {ex.Message}";
            }

            ViewData["Message"] = message;
            return View("Index");
        }

        [HttpPost]
        [MVCAuthorizationFilter(CustomSiteSettingsPermissionProvider.READ_CUSTOMSITESETTINGGS)]
        public async Task<IActionResult> Load([FromForm] string key)
        {
            string? message;
            string? value = null;

            try
            {
                var result = await _keyVaultService.GetData(key);

                value = result;
                message = $"Key '{key}' 的值：";

            }
            catch (Exception ex)
            {
                message = $"读取失败: {ex.Message}";
            }

            ViewData["Message"] = message;
            ViewData["Value"] = value;
            return View("Index");
        }
    }
}
