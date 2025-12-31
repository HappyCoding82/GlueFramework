using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Demo.CustomModule.Attributes;
using Demo.CustomModule.Permissions;

namespace Demo.CustomModule.Controllers
{
    [ApiController]
    [Route("api/tokentest")]
    [Authorize(AuthenticationSchemes = "OpenIddict.Validation.AspNetCore")]
    public class TokenTestController : ControllerBase
    {
        [HttpGet()]
        [RequirePermission(ApiPermissions.ViewTokenTestResourcesPermission)]
        public IActionResult GetTokenTestData()
        {
            return Ok(new
            {
                message = "Token Test API - Successfully accessed with valid token",
                timestamp = DateTime.Now,
                data = new
                {
                    item1 = "Protected Resource 1",
                    item2 = "Protected Resource 2",
                    item3 = "Protected Resource 3"
                }
            });
        }

        [HttpPost]
        [RequirePermission(ApiPermissions.ManageTokenTestSettingsPermission)]
        public IActionResult UpdateTokenTestSettings([FromBody] TestSettings settings)
        {
            if (settings == null)
            {
                return BadRequest("Invalid settings data");
            }

            return Ok(new
            {
                message = "Token Test Settings Updated Successfully",
                timestamp = DateTime.Now,
                updatedSettings = settings
            });
        }

        [HttpGet("admin")]
        [RequirePermission(ApiPermissions.ManageTokenTestSettingsPermission)]
        public IActionResult GetAdminData()
        {
            return Ok(new
            {
                message = "Admin Token Test API - Successfully accessed with valid token and admin permission",
                timestamp = DateTime.Now,
                adminData = new
                {
                    status = "Admin access granted",
                    secretInfo = "This is protected admin information"
                }
            });
        }
    }

    public class TestSettings
    {
        public string Setting1 { get; set; }
        public string Setting2 { get; set; }
        public bool IsActive { get; set; }
    }
}