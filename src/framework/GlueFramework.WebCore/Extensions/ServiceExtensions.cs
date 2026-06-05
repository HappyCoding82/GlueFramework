using GlueFramework.Core.ConfigurationOptions;
using GlueFramework.Core.Security;
using GlueFramework.WebCore.Filters;
using GlueFramework.WebCore.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using GlueFramework.Core.ContextCaches;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace GlueFramework.WebCore.Extensions
{
    public static class ServiceExtensions
    {
        public const string SETTINGS_AZUREAD = "AzureAd";
        public static void AddFrameworkSupport(this IServiceCollection services,IConfiguration configuration, IWebHostEnvironment env)
        {
            
            // //SMTP settings..
            //services.Configure<MailSettings>(Configuration.GetSection("MailSettings")); 
            //services.AddTransient<ISmtpService, SmtpService>();
            //JWT token settings...
            services.Configure<JwtSecurity>(configuration.GetSection("JwtSecurity"));
            
            services.AddTransient(typeof(JwtTokenValidater));
            services.AddTransient(typeof(JwtTokenGenerator));
            services.AddTransient<APIExceptionFilterAttribute>();
            services.AddTransient<IContextCache,MemoryContextCache>();
            services.AddTransient<MemoryCacheInterceptor>();

            //import SqlServer connection...
            //services.UseSqlServerDataAccessor();
            services.AddControllersWithViews(options =>
            {
            });
            //services.AddControllersWithViews();

            var swaggerSection = configuration.GetSection("Swagger");
            if (swaggerSection.Exists())
            {
                services.Configure<SwaggerSettings>(configuration.GetSection("Swagger"));
                var swaggerSettings = swaggerSection.Get<SwaggerSettings>();
                //var xmlFile = typeof(ServiceExtensions).Assembly.FullName + ".xml";
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc(swaggerSettings.APIName, new OpenApiInfo
                    {
                        Title = swaggerSettings.Title,
                        Version = swaggerSettings.Version,
                        Description = swaggerSettings.Description
                    });

                    c.OperationFilter<CustomHeaderSwaggerAttribute>();

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
                    });
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}

                    }
                });
                    var file = Path.Combine(AppContext.BaseDirectory, swaggerSettings.XmlPath);  // xml doc directory
                    var path = Path.Combine(AppContext.BaseDirectory, file); // xml doc file 
                    c.IncludeXmlComments(path, true); // true : display controller desc
                    c.OrderActionsBy(o => o.RelativePath); // sort action by name
                });
            }

            
        }
    }

    public class CustomHeaderSwaggerAttribute : IOperationFilter
    {

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

           
        }

    }
}
