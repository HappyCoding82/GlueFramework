using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;

namespace GlueFramework.AuditLogModule
{
    public sealed class AdminMenu : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
                return ValueTask.CompletedTask;

            builder
                .Add(S["Diagnostics"], "100", diag => diag
                    .Add(S["Audit Logs"], "30", item => item
                        .Action("Index", "AuditLogAdmin", new { area = "GlueFramework.AuditLogModule" })
                        .Permission(Permissions.ManageAuditLogs)));

            return ValueTask.CompletedTask;
        }
    }
}
