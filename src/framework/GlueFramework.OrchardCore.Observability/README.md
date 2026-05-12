# Glue Framework OrchardCore Observability

OpenTelemetry integration for OrchardCore multitenant applications.

## Features

- **Multi-tenant Observability** - Per-tenant OpenTelemetry configuration
- **Automatic Instrumentation** - ASP.NET Core, HTTP client, and runtime metrics
- **OTLP Export** - Send traces and metrics to any OpenTelemetry collector
- **Admin Dashboard** - View runtime state and configuration

## Key Components

- `TenantOpenTelemetryManager` - Manages telemetry per tenant
- `TenantOpenTelemetryHostedService` - Lifecycle management
- `ObservabilityRuntimeState` - Runtime metrics and status

## Configuration

```yaml
# appsettings.yaml
OrchardCore:
  GlueFramework_Observability:
    ObservabilityOptions:
      Enabled: true
      OtlpEndpoint: http://localhost:4317
      ServiceName: MyApplication
```

Or configure per-tenant via admin UI at `/Admin/Observability/Settings`.

## Metrics Collected

- HTTP request duration and count
- Database query performance
- Custom business metrics via `IObservabilityRuntimeState`

Depends on `GlueFramework.OrchardCoreModule`.
