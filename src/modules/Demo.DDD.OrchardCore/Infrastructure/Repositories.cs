using Demo.DDD.OrchardCore.Domain;
using System.Collections.Concurrent;

namespace Demo.DDD.OrchardCore.Infrastructure
{
    public interface IListingRepository
    {
        Task AddAsync(Listing listing);

        Task<Listing?> GetAsync(string listingId);

        Task UpdateAsync(Listing listing);

        Task<List<Listing>> SearchAsync(string? query);
    }

    public interface IOrderRepository
    {
        Task AddAsync(Order order);

        Task<Order?> GetAsync(string orderId);

        Task UpdateAsync(Order order);
    }

    public sealed class InMemoryListingRepository : IListingRepository
    {
        private readonly ConcurrentDictionary<string, Listing> _db = new();

        public Task AddAsync(Listing listing)
        {
            if (!_db.TryAdd(listing.Id, listing))
                throw new InvalidOperationException("Listing already exists.");
            return Task.CompletedTask;
        }

        public Task<Listing?> GetAsync(string listingId)
        {
            _db.TryGetValue(listingId, out var v);
            return Task.FromResult(v);
        }

        public Task UpdateAsync(Listing listing)
        {
            _db[listing.Id] = listing;
            return Task.CompletedTask;
        }

        public Task<List<Listing>> SearchAsync(string? query)
        {
            var q = (query ?? string.Empty).Trim();
            var results = _db.Values
                .Where(x => string.IsNullOrWhiteSpace(q) || x.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id)
                .ToList();
            return Task.FromResult(results);
        }
    }

    public sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly ConcurrentDictionary<string, Order> _db = new();

        public Task AddAsync(Order order)
        {
            if (!_db.TryAdd(order.Id, order))
                throw new InvalidOperationException("Order already exists.");
            return Task.CompletedTask;
        }

        public Task<Order?> GetAsync(string orderId)
        {
            _db.TryGetValue(orderId, out var v);
            return Task.FromResult(v);
        }

        public Task UpdateAsync(Order order)
        {
            _db[order.Id] = order;
            return Task.CompletedTask;
        }
    }
}
