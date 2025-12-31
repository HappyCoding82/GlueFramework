using Microsoft.AspNetCore.Mvc;
using Demo.DDD.OrchardCore.Application;
using Demo.DDD.OrchardCore.Application.Dtos;

namespace Demo.DDD.OrchardCore.Controllers
{
    [ApiController]
    [Route("api/cards-demo")]
    public sealed class MarketplaceController : ControllerBase
    {
        private readonly IMarketplaceAppService _app;

        public MarketplaceController(IMarketplaceAppService app)
        {
            _app = app;
        }

        [HttpPost("listings")]
        public Task<ListingDto> CreateListing([FromBody] CreateListingRequestDto request)
            => _app.CreateListingAsync(request);

        [HttpGet("listings")]
        public Task<List<ListingDto>> Search([FromQuery] string? q)
            => _app.SearchListingsAsync(q);

        [HttpPost("orders")]
        public Task<OrderDto> PlaceOrder([FromBody] PlaceOrderRequestDto request)
            => _app.PlaceOrderAsync(request);

        [HttpPost("orders/{orderId}/seller-shipped")]
        public Task<OrderDto> SellerShippedToPlatform([FromRoute] string orderId)
            => _app.SellerShippedToPlatformAsync(orderId);

        [HttpPost("orders/{orderId}/verify")]
        public Task<OrderDto> Verify([FromRoute] string orderId, [FromBody] VerifyItemRequestDto request)
            => _app.VerifyItemAsync(orderId, request);

        [HttpPost("orders/{orderId}/ship-to-buyer")]
        public Task<OrderDto> ShipToBuyer([FromRoute] string orderId)
            => _app.ShipToBuyerAsync(orderId);

        [HttpGet("outbox")]
        public ActionResult<List<OutboxItemDto>> Outbox()
            => Ok(_app.GetOutbox());
    }
}
