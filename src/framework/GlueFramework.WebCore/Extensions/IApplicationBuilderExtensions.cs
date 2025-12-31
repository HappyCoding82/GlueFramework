 
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using GlueFramework.WebCore.Models;
using System.Linq;

namespace GlueFramework.WebCore.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static void SupportFramework(this IApplicationBuilder app,IWebHostEnvironment env,IConfiguration configuration)
        {

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapRazorPages();
                //MVC deault route..
                endpoints.MapControllerRoute(
                     name: "default",
                     pattern: "{controller=Home}/{action=Index}/{id?}",
                     defaults: new { controller = "Home", action = "Index" }
                   );
            });

            var swaggerSection = configuration.GetSection("Swagger");
            if (swaggerSection.Exists())
            {
                var swaggerSettings = swaggerSection.Get<SwaggerSettings>();

                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{swaggerSettings.APIName}/swagger.json",
                                 swaggerSettings.Description));
            }
        }
    }
}
