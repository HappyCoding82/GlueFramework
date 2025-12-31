using Demo.DDD.OrchardCore.Application.Dtos;

namespace Demo.DDD.OrchardCore.Application
{
    public interface IMarketplaceAppService
    {
        Task<ListingDto> CreateListingAsync(CreateListingRequestDto request);

        Task<List<ListingDto>> SearchListingsAsync(string? query);

        Task<OrderDto> PlaceOrderAsync(PlaceOrderRequestDto request);

        Task<OrderDto> SellerShippedToPlatformAsync(string orderId);

        Task<OrderDto> VerifyItemAsync(string orderId, VerifyItemRequestDto request);

        Task<OrderDto> ShipToBuyerAsync(string orderId);

        List<OutboxItemDto> GetOutbox();
    }
}
