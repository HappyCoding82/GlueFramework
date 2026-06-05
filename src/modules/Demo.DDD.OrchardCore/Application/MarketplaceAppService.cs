using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;
using Demo.DDD.OrchardCore.Application.Dtos;
using Demo.DDD.OrchardCore.Domain;
using Demo.DDD.OrchardCore.Infrastructure;

namespace Demo.DDD.OrchardCore.Application
{
    public sealed class MarketplaceAppService : ServiceBase, IMarketplaceAppService
    {
        private readonly IEventBus _eventBus;
        private readonly IListingRepository _listingRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly InMemoryOutbox _outbox;

        public MarketplaceAppService(
            IDbConnectionAccessor dbConnAccessor,
            IDataTablePrefixProvider dataTablePrefixProvider,
            IEventBus eventBus,
            IListingRepository listingRepo,
            IOrderRepository orderRepo,
            InMemoryOutbox outbox)
            : base(dbConnAccessor, dataTablePrefixProvider)
        {
            _eventBus = eventBus;
            _listingRepo = listingRepo;
            _orderRepo = orderRepo;
            _outbox = outbox;
        }

        [Transactional]
        public async Task<ListingDto> CreateListingAsync(CreateListingRequestDto request)
        {
            var listing = Listing.Create(request.SellerId, request.Title, request.Price, request.ListingImageRef);
            await _listingRepo.AddAsync(listing);

            foreach (var evt in listing.DequeueEvents())
                await _eventBus.PublishAfterCommitAsync((dynamic)evt);

            return new ListingDto(listing.Id, listing.SellerId, listing.Title, listing.Price, listing.Status);
        }

        public async Task<List<ListingDto>> SearchListingsAsync(string? query)
        {
            var list = await _listingRepo.SearchAsync(query);
            return list.Select(x => new ListingDto(x.Id, x.SellerId, x.Title, x.Price, x.Status)).ToList();
        }

        [Transactional]
        public async Task<OrderDto> PlaceOrderAsync(PlaceOrderRequestDto request)
        {
            var listing = await _listingRepo.GetAsync(request.ListingId) ?? throw new InvalidOperationException("Listing not found.");
            listing.MarkSold();
            await _listingRepo.UpdateAsync(listing);

            var order = Order.Place(listing.Id, request.BuyerId, listing.SellerId, listing.Price);
            await _orderRepo.AddAsync(order);

            foreach (var evt in order.DequeueEvents())
                await _eventBus.PublishAfterCommitAsync((dynamic)evt);

            return new OrderDto(order.Id, order.ListingId, order.BuyerId, order.SellerId, order.Status, order.PaymentIntentId);
        }

        [Transactional]
        public async Task<OrderDto> SellerShippedToPlatformAsync(string orderId)
        {
            var order = await _orderRepo.GetAsync(orderId) ?? throw new InvalidOperationException("Order not found.");
            order.SellerShippedToPlatform();
            await _orderRepo.UpdateAsync(order);

            foreach (var evt in order.DequeueEvents())
                await _eventBus.PublishAfterCommitAsync((dynamic)evt);

            return new OrderDto(order.Id, order.ListingId, order.BuyerId, order.SellerId, order.Status, order.PaymentIntentId);
        }

        [Transactional]
        public async Task<OrderDto> VerifyItemAsync(string orderId, VerifyItemRequestDto request)
        {
            var order = await _orderRepo.GetAsync(orderId) ?? throw new InvalidOperationException("Order not found.");
            order.Verify(request.Ok, request.Notes);
            await _orderRepo.UpdateAsync(order);

            foreach (var evt in order.DequeueEvents())
                await _eventBus.PublishAfterCommitAsync((dynamic)evt);

            return new OrderDto(order.Id, order.ListingId, order.BuyerId, order.SellerId, order.Status, order.PaymentIntentId);
        }

        [Transactional]
        public async Task<OrderDto> ShipToBuyerAsync(string orderId)
        {
            var order = await _orderRepo.GetAsync(orderId) ?? throw new InvalidOperationException("Order not found.");
            order.ShipToBuyer();
            await _orderRepo.UpdateAsync(order);

            foreach (var evt in order.DequeueEvents())
                await _eventBus.PublishAfterCommitAsync((dynamic)evt);

            return new OrderDto(order.Id, order.ListingId, order.BuyerId, order.SellerId, order.Status, order.PaymentIntentId);
        }

        public List<OutboxItemDto> GetOutbox()
        {
            return _outbox.Snapshot().Select(x => new OutboxItemDto(x.Type, x.Payload)).ToList();
        }
    }
}
