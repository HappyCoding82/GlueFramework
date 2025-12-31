using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;

namespace Demo.DDD.OrchardCore
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
                .Add(S["Demo"], "990", demo => demo
                    .Add(S["Outbox Test"], "10", item => item
                        .Action("Index", "OutboxTestAdmin", new { area = "Demo.DDD.OrchardCore" })
                        .Permission(Permissions.ManageDemoOutbox)));

            return ValueTask.CompletedTask;
        }
    }
}
