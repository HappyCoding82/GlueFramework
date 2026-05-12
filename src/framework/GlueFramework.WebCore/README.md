# Glue Framework Web Core

Web layer abstractions and utilities for ASP.NET Core applications.

## Features

### Exception Handling

- `BusinessException` / `BusinessAcceptedException` - Domain-level exceptions with HTTP status mapping
- `APIExceptionFilterAttribute` - Global exception filter for API controllers
- `UnauthorizedTenantException` / `RoleNotPermittedException` - Authorization exceptions

### Validation Framework

- `ValidatorBase<T>` - Base class for FluentValidation-style validators
- `ClientValidationAttribute` - Client-side validation support
- `ValidatorFactoryAttribute` - Automatic validator discovery

### Extension Methods

- `HttpContextExtensions` - HTTP context utilities (tenant resolution, correlation IDs)
- `IApplicationBuilderExtensions` - Middleware pipeline configuration helpers
- `IHostBuilderExtensions` - Host configuration helpers

### Swagger Integration

Built-in Swagger configuration with:
- JWT authentication support
- XML documentation generation
- Multi-tenant API grouping

## Usage

```csharp
// In Startup.cs
public void Configure(IApplicationBuilder app)
{
    app.UseGlueFrameworkWebCore(); // Extension from this package
    app.UseSwaggerWithJwtAuth();   // Swagger with auth
}

// Validation example
public class CreateOrderValidator : ValidatorBase<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

Depends on `GlueFramework.Core`.