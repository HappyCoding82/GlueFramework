namespace Demo.DDD.OrchardCore.Application.Dtos
{
    public sealed record CreateListingRequestDto(string SellerId, string Title, decimal Price, string ListingImageRef);

    public sealed record ListingDto(string ListingId, string SellerId, string Title, decimal Price, string Status);

    public sealed record PlaceOrderRequestDto(string BuyerId, string ListingId);

    public sealed record VerifyItemRequestDto(bool Ok, string? Notes);

    public sealed record OrderDto(string OrderId, string ListingId, string BuyerId, string SellerId, string Status, string? PaymentIntentId);

    public sealed record OutboxItemDto(string Type, string Payload);
}
