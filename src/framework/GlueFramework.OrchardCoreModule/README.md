# Glue Framework OrchardCore Module

Base module providing core OrchardCore integration for Glue Framework.

## Features

### Multi-tenancy Support

- **Tenant-aware Services** - Automatic tenant resolution
- **Per-tenant Configuration** - Settings isolated by tenant
- **Tenant Table Prefix** - Database table prefixing per tenant

### Data Access

- **DALFactory** - Data Access Layer factory with DI integration
- **TenantTablePrefixProvider** - Dynamic table prefix resolution
- **YesSql Integration** - OrchardCore's document database abstraction

### GraphQL Support

- `RootQuery` / `RootMutation` - GraphQL schema foundation
- **Filtering** - Generic entity filtering with GraphQL

### Module Infrastructure

- **Permission Providers** - Declarative permission registration
- **Admin Menu** - OrchardCore admin navigation integration
- **Background Tasks** - Scheduled task support

## Usage

```csharp
public class MyModule : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register DAL with tenant support
        services.AddScoped<IMyDAL, MyDAL>();
        
        // Add admin menu
        services.AddScoped<INavigationProvider, MyAdminMenu>();
    }
}
```

## Dependencies

- `GlueFramework.Core` - Foundation services
- `GlueFramework.WebCore` - Web utilities
- OrchardCore 2.2.1 modules

This is a foundational package - most other GlueFramework modules depend on it.
