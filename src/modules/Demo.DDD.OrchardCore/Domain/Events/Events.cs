namespace Demo.DDD.OrchardCore.Domain.Events
{
    public sealed record ListingCreated(string ListingId, string SellerId, string Title, decimal Price);

    public sealed record OrderPlaced(string OrderId, string ListingId, string BuyerId, string SellerId, decimal Price);

    public sealed record SellerShippedToPlatform(string OrderId);

    public sealed record ItemVerifiedOk(string OrderId);

    public sealed record ItemVerifiedFailed(string OrderId, string? Notes);

    public sealed record ShippedToBuyer(string OrderId);

    public sealed record SellerPaid(string OrderId, string SellerId, decimal Amount, string PayoutId);
}
