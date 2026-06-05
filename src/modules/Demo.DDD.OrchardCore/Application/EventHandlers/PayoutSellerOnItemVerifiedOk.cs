using GlueFramework.Core.Abstractions;
using Demo.DDD.OrchardCore.Domain.Events;
using Demo.DDD.OrchardCore.Infrastructure;

namespace Demo.DDD.OrchardCore.Application.EventHandlers
{
    public sealed class PayoutSellerOnItemVerifiedOk : IEventHandler<ItemVerifiedOk>
    {
        private readonly IOrderRepository _orders;
        private readonly IStripePayments _stripe;
        private readonly IEventBus _bus;

        public PayoutSellerOnItemVerifiedOk(IOrderRepository orders, IStripePayments stripe, IEventBus bus)
        {
            _orders = orders;
            _stripe = stripe;
            _bus = bus;
        }

        public async Task HandleAsync(ItemVerifiedOk evt, CancellationToken cancellationToken = default)
        {
            var order = await _orders.GetAsync(evt.OrderId).ConfigureAwait(false);
            if (order == null)
                return;

            if (order.Status == "SellerPaid")
                return;

            var payoutId = await _stripe.CreatePayoutAsync(order.Id, order.SellerId, order.Price).ConfigureAwait(false);
            order.MarkSellerPaid(payoutId);
            await _orders.UpdateAsync(order).ConfigureAwait(false);

            foreach (var e in order.DequeueEvents())
            {
                await _bus.PublishNowBestEffortAsync((dynamic)e, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
