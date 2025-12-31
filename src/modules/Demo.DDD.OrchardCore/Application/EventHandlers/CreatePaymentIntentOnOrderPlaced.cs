using GlueFramework.Core.Abstractions;
using Demo.DDD.OrchardCore.Domain.Events;
using Demo.DDD.OrchardCore.Infrastructure;

namespace Demo.DDD.OrchardCore.Application.EventHandlers
{
    public sealed class CreatePaymentIntentOnOrderPlaced : IEventHandler<OrderPlaced>
    {
        private readonly IOrderRepository _orders;
        private readonly IStripePayments _stripe;

        public CreatePaymentIntentOnOrderPlaced(IOrderRepository orders, IStripePayments stripe)
        {
            _orders = orders;
            _stripe = stripe;
        }

        public async Task HandleAsync(OrderPlaced evt, CancellationToken cancellationToken = default)
        {
            var order = await _orders.GetAsync(evt.OrderId).ConfigureAwait(false);
            if (order == null)
                return;

            if (!string.IsNullOrWhiteSpace(order.PaymentIntentId))
                return;

            var pi = await _stripe.CreatePaymentIntentAsync(evt.OrderId, evt.Price).ConfigureAwait(false);
            order.AttachPaymentIntent(pi);
            await _orders.UpdateAsync(order).ConfigureAwait(false);
        }
    }
}
