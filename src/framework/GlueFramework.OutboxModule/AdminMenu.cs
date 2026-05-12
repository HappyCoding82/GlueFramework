using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;

namespace GlueFramework.OutboxModule
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
                .Add(S["Configuration"], "100", config => config
                    .Add(S["Outbox"], "50", item => item
                        .Permission(Permissions.ManageOutbox)
                        .Add(S["Records"], "5", sub => sub
                            .Action("Index", "OutboxRecordsAdmin", new { area = "GlueFramework.OutboxModule" })
                            .Permission(Permissions.ManageOutbox))
                        .Add(S["Settings"], "10", sub => sub
                            .Action("Index", "OutboxSettingsAdmin", new { area = "GlueFramework.OutboxModule" })
                            .Permission(Permissions.ManageOutbox))
                        .Add(S["Archive"], "20", sub => sub
                            .Action("Index", "OutboxArchiveAdmin", new { area = "GlueFramework.OutboxModule" })
                            .Permission(Permissions.ManageOutbox))));

            return ValueTask.CompletedTask;
        }
    }
}
