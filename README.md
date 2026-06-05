"# GlueFramework"

## A framework for modular DDD development based on Orchard Core,
Support Linq to dapper

- **LINQ to Dapper** - Type-safe SQL query builder with multi-table Joins, aggregation, and grouping
- **Modular DDD** - Modular architecture based on Orchard Core
- **AOP Support** - Declarative transactions, caching, and audit logging

---

## Table of Contents

1. [LINQ to Dapper](#linq-to-dapper)
   - [Basic Queries](#basic-queries)
   - [Multi-table Joins](#multi-table-joins)
   - [Aggregation Functions](#aggregation-functions)
   - [Session Modes](#session-modes)
2. [Caching Support](#caching-support)
   - [Declarative Caching](#declarative-caching)
   - [Programmatic Caching](#programmatic-caching)
   - [Cache Invalidation](#cache-invalidation)
3. [Transaction Support](#transaction-support)
   - [Declarative Transactions](#declarative-transactions)
   - [Transaction Hooks](#transaction-hooks)
4. [Observability Support](#observability-support)
   - [Slow SQL Monitoring](#slow-sql-monitoring)
   - [OpenTelemetry Integration](#opentelemetry-integration)
5. [NuGet Cache Cleanup](#nuget-cache-cleanup)

---

## LINQ to Dapper

### Basic Queries

```csharp
using var s = OpenJoinQuerySessionScope();

var products = await s.Session
    .From<Product>()
    .Select(dto => new ProductDto { Id = dto.Product.Id, Name = dto.Product.Name })
    .ToListAsync(s.Connection);
```

### Multi-table Joins

```csharp
using var s = OpenJoinQuerySessionScope();

var result = await s.Session
    .From<Order>()
    .Join<Customer>((o, c) => o.CustomerId == c.Id)
    .Join<OrderItem>((o, c, i) => o.Id == i.OrderId)
    .Select(dto => new OrderDetailDto
    {
        OrderId = dto.Order.Id,
        CustomerName = dto.Customer.Name,
        ItemCount = SqlFn.Count()
    })
    .GroupBy((o, c, i) => new { o.Id, c.Name })
    .ToListAsync(s.Connection);
```

### Aggregation Functions

The framework supports the following SQL aggregation functions:

| Function | Description | Example |
|----------|-------------|---------|
| `SqlFn.Count()` | Count all | `SqlFn.Count()` |
| `SqlFn.Count(column)` | Non-null count | `SqlFn.Count(dto.Product.Id)` |
| `SqlFn.Sum(column)` | Sum | `SqlFn.Sum(dto.Order.Amount)` |
| `SqlFn.Avg(column)` | Average | `SqlFn.Avg(dto.Product.Price)` |
| `SqlFn.Min(column)` | Minimum | `SqlFn.Min(dto.Order.Date)` |
| `SqlFn.Max(column)` | Maximum | `SqlFn.Max(dto.Order.Amount)` |
| `SqlFn.Distinct(column)` | Distinct | `SqlFn.Distinct(dto.Product.Category)` |

**GroupBy + Sum Example:**

```csharp
var report = await s.Session
    .From<Order>()
    .Join<Customer>((o, c) => o.CustomerId == c.Id)
    .Select(dto => new CustomerReportDto
    {
        CustomerName = dto.Customer.Name,
        TotalAmount = SqlFn.Sum(dto.Order.Amount),
        OrderCount = SqlFn.Count()
    })
    .GroupBy((o, c) => c.Name)           // GROUP BY Customer.Name
    .Having((o, c) => SqlFn.Sum(o.Amount) > 1000)  // HAVING SUM(Amount) > 1000
    .OrderBy((o, c) => SqlFn.Sum(o.Amount), desc: true)
    .ToListAsync(s.Connection);
```

### Session Modes

| Method | Use Case |
|--------|----------|
| `OpenDbSessionScope()` | Standard DAL operations |
| `OpenJoinQuerySessionScope()` | LINQ JoinQuery queries |
| `OpenJoinQuerySessionScope(dbScope)` | Reuse existing DbSessionScope |

**Creating JoinQuery from DbSessionScope:**

```csharp
using var dbScope = OpenDbSessionScope();
using var joinScope = OpenJoinQuerySessionScope(dbScope);

var query = joinScope.Session.From<Product>()...;
```

---

## Caching Support

The framework provides declarative AOP caching via **Castle DynamicProxy**, built on `IMemoryCache`.

### Declarative Caching

Mark methods with the `[ContextCache]` attribute to automatically cache return values:

```csharp
public interface IProductService
{
    // Cache query results
    [ContextCache("products:all")]
    Task<List<ProductDto>> GetAllAsync();

    // Dynamic cache key with prefix
    [ContextCache("product:{id}")]
    Task<ProductDto?> GetByIdAsync(int id);

    // Remove cache (after data modification)
    [ContextCache("products:all", isRemoval: true)]
    Task UpdateAsync(ProductDto dto);
}
```

**Service Registration (Auto-Proxy):**

```csharp
// Startup.cs / Module configuration
services.AddContextCachedScoped<IProductService, ProductService>();

// Or auto-detect attributes (recommended)
services.AddProxiesScopedAuto<IProductService, ProductService>();
```

### Programmatic Caching

Inject `IContextCache` directly for manual cache operations:

```csharp
public class ProductService : IProductService
{
    private readonly IContextCache _cache;

    public ProductService(IContextCache cache)
    {
        _cache = cache;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var key = $"product:{id}";

        // Try to get from cache
        var cached = _cache.Get<ProductDto>(key);
        if (cached != null) return cached;

        // Query from database
        var product = await _db.QueryAsync<ProductDto>(...);

        // Store in cache (default sliding expiration: 1 minute)
        _cache.Set(key, product, slidingExpirationMinutes: 5);

        return product;
    }
}
```

### Cache Invalidation

| Attribute Parameter | Description |
|---------------------|-------------|
| `key` | Cache key, supports `{param}` placeholders |
| `isRemoval` | When `true`, removes cache instead of writing |

**Transaction Consistency:**

Cache removal operations are deferred until the transaction commits, ensuring data consistency:

```csharp
[ContextCache("products:all", isRemoval: true)]  // Actually removed after commit
[Transactional]
public async Task UpdateAsync(ProductDto dto)
{
    // Database update...
    await _dal.UpdateAsync(dto);
    // Cache removal deferred until transaction commits
}
```

### Cache Key Prefix

Implement the `ICacheKeyPrefixProvider` interface to add a unified prefix to cache keys:

```csharp
public class TenantService : ICacheKeyPrefixProvider
{
    public string? CacheKeyPrefix => $"tenant:{_tenantId}";

    [ContextCache("products")]  // Actual key: tenant:123:products
    public async Task<List<ProductDto>> GetProductsAsync() { ... }
}
```

### Register Cache Infrastructure

```csharp
// Required: Register IMemoryCache
services.AddMemoryCache();

// Required: Register IContextCache and interceptor
services.AddTransient<IContextCache, MemoryContextCache>();
services.AddTransient<MemoryCacheInterceptor>();
```

---

## Transaction Support

The framework provides declarative transaction management via AOP interceptors, automatically handling commit/rollback for both synchronous and asynchronous methods.

### Declarative Transactions

Use the `[Transactional]` attribute to wrap method execution in a database transaction:

```csharp
public interface IOrderService
{
    [Transactional]
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);

    [Transactional]
    Task TransferFundsAsync(int fromAccount, int toAccount, decimal amount);
}
```

**Features:**

| Feature | Description |
|---------|-------------|
| **Auto Commit** | Commit on success, rollback on exception |
| **Nested Transaction Detection** | Prevents duplicate transactions within same service |
| **Async Support** | Works with `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` |
| **AOP-based** | No manual transaction management needed |

**Service Registration:**

```csharp
// Register with transactional proxy
services.AddTransactionalScoped<IOrderService, OrderService>();

// Or auto-detect all AOP attributes (recommended)
services.AddProxiesScopedAuto<IOrderService, OrderService>();
```

### Transaction Hooks

Execute code after transaction commits using `TransactionScopeContext`:

```csharp
[Transactional]
public async Task UpdateInventoryAsync(int productId, int quantity)
{
    // Database operations...
    await _inventoryDal.UpdateAsync(productId, quantity);

    // Enqueue action to run AFTER transaction commits
    TransactionScopeContext.EnqueueAfterCommit(() =>
    {
        // This runs only if transaction succeeds
        _eventBus.Publish(new InventoryUpdatedEvent
        {
            ProductId = productId,
            NewQuantity = quantity
        });
    });
}
```

**Common Use Cases for After-Commit Hooks:**

| Use Case | Example |
|----------|---------|
| **Cache Invalidation** | Remove cached data after DB update commits |
| **Event Publishing** | Publish domain events after transaction success |
| **Notifications** | Send emails/notifications after data persists |
| **Audit Logging** | Log successful changes to external systems |

**Checking Active Transaction:**

```csharp
public void SomeMethod()
{
    if (TransactionScopeContext.HasActiveScope)
    {
        // Currently inside a transaction
        TransactionScopeContext.EnqueueAfterCommit(() =>
        {
            Console.WriteLine("Transaction completed!");
        });
    }
}
```

### Manual Transaction Control

For scenarios requiring explicit transaction management, use `BeginTransaction()` in services inheriting from `ServiceBase`:

```csharp
public class OrderService : ServiceBase
{
    public OrderService(IDbConnectionAccessor accessor) : base(accessor) { }

    public async Task ProcessOrderAsync(OrderDto order)
    {
        // Begin manual transaction
        using var tx = BeginTransaction();

        try
        {
            using var scope = OpenDbSessionScope();
            var dal = GetDAL(scope);

            await dal.InsertAsync(order);

            // Commit transaction
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
```

**Transaction-Aware Session Scopes:**

```csharp
// Within a transaction, OpenDbSessionScope() returns the transaction connection
using var tx = BeginTransaction();

// This scope uses the transaction connection automatically
using var scope = OpenDbSessionScope();
// All operations in this scope participate in the transaction
```

### Interceptor Execution Order

When using multiple AOP features together, interceptors execute in this order (outer to inner):

```
Audit/AuditStep → Cache → Transaction
```

This ensures:
1. **Audit** records even cache hits
2. **Cache** is checked before starting a transaction
3. **Transaction** wraps the actual database operation

---

## Observability Support

The framework provides comprehensive observability features including Slow SQL monitoring and OpenTelemetry integration.

### Slow SQL Monitoring

Automatically detect and log slow database queries with configurable thresholds.

**Configuration:**

```csharp
// appsettings.json
{
  "SlowSqlOptions": {
    "Enabled": true,
    "ThresholdMs": 200,              // Log queries slower than 200ms
    "LogOnError": true,              // Also log SQL errors
    "IncludeTrace": true,            // Include TraceId/SpanId
    "IncludeDatabase": true,         // Include database name
    "IncludeParameters": false,      // Include parameter values
    "SensitiveParameterNames": ["password", "token", "secret"],
    "MaxParameterValueLength": 256,
    "MaxCommandTextLength": 4096
  }
}
```

**Features:**

| Feature | Description |
|---------|-------------|
| **ProfilingDbCommand** | Wraps DbCommand to measure execution time |
| **ProfilingDbConnection** | Creates profiling commands automatically |
| **Log Levels** | Slow queries → Warning, SQL errors → Error |
| **OpenTelemetry Integration** | Captures TraceId and SpanId automatically |

**Log Output Example:**

```
SlowSql ElapsedMs=350 Db=MyDatabase TraceId=abc123 SpanId=def456 Sql=SELECT * FROM Orders WHERE...
```

### OpenTelemetry Integration

Multi-tenant OpenTelemetry support via the `GlueFramework.OrchardCore.Observability` module.

**Features:**

| Feature | Description |
|---------|-------------|
| **Multi-tenant** | Per-tenant telemetry configuration |
| **Traces** | ASP.NET Core, HTTP client, custom activities |
| **Metrics** | Runtime metrics (GC, memory, thread pool) |
| **OTLP Export** | Export to any OpenTelemetry Collector |
| **Sampling** | Configurable trace sampling rate |

**Configuration:**

```yaml
# appsettings.yaml
OrchardCore:
  GlueFramework_Observability:
    ObservabilityOptions:
      Enabled: true
      OtlpEndpoint: http://localhost:4317
      TraceSampleRate: 0.01              # 1% sampling
      EnableAspNetCoreInstrumentation: true
      EnableHttpClientInstrumentation: true
      EnableRuntimeMetrics: true
      DashboardUrl: https://jaeger.mydomain.com
      TracesUrl: https://jaeger.mydomain.com/traces
      MetricsUrl: https://prometheus.mydomain.com
```

**Per-tenant Admin UI:**

Navigate to `/Admin/Observability/Settings` to configure per-tenant:
- Enable/disable telemetry
- OTLP endpoint override
- Trace sampling rate
- Instrumentation toggles

**Programmatic Usage:**

```csharp
// Access runtime state
public class MyService
{
    private readonly ObservabilityRuntimeState _state;

    public MyService(ObservabilityRuntimeState state)
    {
        _state = state;
    }

    public void CheckStatus()
    {
        if (_state.Started)
        {
            Console.WriteLine($"OTLP Endpoint: {_state.AppliedOtlpEndpoint}");
            Console.WriteLine($"Trace Rate: {_state.AppliedTraceSampleRate}");
        }
    }
}
```

**Instrumentation Sources:**

| Source | Data Collected |
|--------|----------------|
| `AspNetCore` | HTTP requests, response times, status codes |
| `HttpClient` | Outgoing HTTP calls |
| `Runtime` | GC collections, memory usage, thread pool |
| `Framework.Core` | Custom business activities |

### Recommended Setup

For production observability, enable both Slow SQL and OpenTelemetry:

```csharp
// Program.cs or Startup.cs

// 1. Add Slow SQL monitoring
builder.Services.Configure<SlowSqlOptions>(options =>
{
    options.Enabled = true;
    options.ThresholdMs = 200;
    options.IncludeTrace = true;
});

// 2. Add OpenTelemetry (via OrchardCore module)
builder.Services.AddOrchardCore()
    .AddTenantFeatures<GlueFramework.OrchardCore.Observability.Startup>();
```

---

## NuGet Cache Cleanup

```bash
clear_nuget_cache_package.bat --glue-all