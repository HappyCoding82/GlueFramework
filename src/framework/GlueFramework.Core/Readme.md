# Glue Framework Core

The foundational package of Glue Framework providing DDD building blocks and data access abstractions.

## Features

### Repository Pattern with LINQ

```csharp
// Query with lambda
var items = await _repository.QueryAsync(x => x.Category != "");

// Query top N
var top3 = await _repository.QueryTopAsync(x => x.IsActive, 3);

// Paged search with filter options
var page = await _repository.PagerSearchAsync(
    new FilterOptions<MyEntity>(
        x => x.Status == Status.Active,
        new PagerInfo { PageIndex = 1, PageSize = 20 }
    )
);
```

### DDD Building Blocks

- **Entity & ValueObject** - Base classes for domain models
- **AggregateRoot** - Transaction boundary markers
- **DomainEvent** - In-process event publishing
- **UnitOfWork** - Transaction management

### Data Access

- **IDbSession** - Database session abstraction
- **IDALFactory** - Data Access Layer factory
- **IRepository<T>** - Generic repository interface

### Cross-Cutting Concerns

- **IIdentityProvider** - User identity abstraction
- **IHashService** - Cryptographic hashing
- **ICacheManager** - Caching abstraction

## Usage

```csharp
public class OrderService
{
    private readonly IRepository<Order> _orderRepo;
    private readonly IUnitOfWork _uow;
    
    public OrderService(IRepository<Order> orderRepo, IUnitOfWork uow)
    {
        _orderRepo = orderRepo;
        _uow = uow;
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order(dto.CustomerId, dto.Items);
        await _orderRepo.AddAsync(order);
        await _uow.CommitAsync();
        return order;
    }
}
```

## Dependencies

- Autofac for DI
- Dapper for data access
- Newtonsoft.Json for serialization