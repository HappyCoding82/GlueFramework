using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Demo.CustomModule.Attributes;
using Demo.CustomModule.Permissions;

namespace Demo.CustomModule.Controllers
{

    [ApiController]
    [Route("api/products")]
    [Authorize(AuthenticationSchemes = "OpenIddict.Validation.AspNetCore")]
    public class ProductController : ControllerBase
    {
        [HttpGet]
        [RequirePermission(ApiPermissions.ViewProductsPermission)]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Products",
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
        [RequirePermission(ApiPermissions.ManageProductsPermission)]
        public IActionResult Create() => Ok("Product created successfully");

        [HttpGet("view")]
        [RequirePermission(ApiPermissions.ViewProductsPermission)]
        public IActionResult View()
        {
            return Ok(new
            {
                message = "Product details",
                timestamp = DateTime.Now, 
            });
        }

    }
}