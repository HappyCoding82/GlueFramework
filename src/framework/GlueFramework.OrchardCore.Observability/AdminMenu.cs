using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;

namespace GlueFramework.OrchardCore.Observability
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
                .Add(S["Observability"], "100", obs => obs
                    .AddClass("observability")
                    .LinkToFirstChild(true)
                    .Add(S["Status"], "0", item => item
                        .Action("Index", "ObservabilityAdmin", new { area = "GlueFramework.OrchardCore.Observability" })
                        .Permission(Permissions.ManageObservability))
                    .Add(S["Settings"], "1", item => item
                        .Action("Index", "ObservabilitySettings", new { area = "GlueFramework.OrchardCore.Observability" })
                        .Permission(Permissions.ManageObservability)));

            return ValueTask.CompletedTask;
        }
    }
}
