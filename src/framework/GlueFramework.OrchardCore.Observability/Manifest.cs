using OrchardCore.Modules.Manifest;

[assembly: OrchardCore.Modules.Manifest.Module(
    Name = "GlueFramework.OrchardCore.Observability",
    Author = "GlueFramework",
    Website = "",
    Version = "0.0.1",
    Description = "Observability (OpenTelemetry) integration.",
    Category = "Diagnostics",
    Dependencies = new string[] { "GlueFramework.OrchardCoreModule" }
)]
