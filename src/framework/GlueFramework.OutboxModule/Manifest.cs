using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "GlueFramework.OutboxModule",
    Author = "GlueFramework",
    Website = "https://glueframework.com",
    Version = "0.0.1",
    Description = "Outbox + Inbox module.",
    Category = "Infrastructure",
    Dependencies = new[] { "GlueFramework.OrchardCoreModule" }
)]
