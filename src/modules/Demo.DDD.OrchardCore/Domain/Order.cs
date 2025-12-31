using System;
using System.Linq;
using Demo.DDD.OrchardCore.Domain.Events;

namespace Demo.DDD.OrchardCore.Domain
{
    public sealed class Order
    {
        private readonly List<object> _events = new();

        private Order(string id, string listingId, string buyerId, string sellerId, decimal price)
        {
            Id = id;
            ListingId = listingId;
            BuyerId = buyerId;
            SellerId = sellerId;
            Price = price;
            Status = "Placed";
        }

        public string Id { get; }

        public string ListingId { get; }

        public string BuyerId { get; }

        public string SellerId { get; }

        public decimal Price { get; }

        public string Status { get; private set; }

        public string? PaymentIntentId { get; private set; }

        public IReadOnlyList<object> DomainEvents => _events;

        public IReadOnlyList<object> DequeueEvents()
        {
            if (_events.Count == 0)
                return Array.Empty<object>();

            var copy = _events.ToArray();
            _events.Clear();
            return copy;
        }

        public static Order Place(string listingId, string buyerId, string sellerId, decimal price)
        {
            if (string.IsNullOrWhiteSpace(listingId)) throw new ArgumentException("ListingId required", nameof(listingId));
            if (string.IsNullOrWhiteSpace(buyerId)) throw new ArgumentException("BuyerId required", nameof(buyerId));
            if (string.IsNullOrWhiteSpace(sellerId)) throw new ArgumentException("SellerId required", nameof(sellerId));
            if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));

            var id = "O-" + Guid.NewGuid().ToString("N");
            var order = new Order(id, listingId, buyerId, sellerId, price);
            order.AddEvent(new OrderPlaced(id, listingId, buyerId, sellerId, price));
            return order;
        }

        public void AttachPaymentIntent(string paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId)) throw new ArgumentException("PaymentIntentId required", nameof(paymentIntentId));
            PaymentIntentId = paymentIntentId;
        }

        public void SellerShippedToPlatform()
        {
            if (Status != "Placed") throw new InvalidOperationException("Order must be Placed.");
            Status = "SellerShippedToPlatform";
            AddEvent(new SellerShippedToPlatform(Id));
        }

        public void Verify(bool ok, string? notes)
        {
            if (Status != "SellerShippedToPlatform") throw new InvalidOperationException("Order must be shipped to Platform first.");

            if (ok)
            {
                Status = "VerifiedOk";
                AddEvent(new ItemVerifiedOk(Id));
            }
            else
            {
                Status = "VerifiedFailed";
                AddEvent(new ItemVerifiedFailed(Id, notes));
            }
        }

        public void ShipToBuyer()
        {
            if (Status != "VerifiedOk") throw new InvalidOperationException("Order must be VerifiedOk.");
            Status = "ShippedToBuyer";
            AddEvent(new ShippedToBuyer(Id));
        }

        public void MarkSellerPaid(string payoutId)
        {
            if (Status != "VerifiedOk") throw new InvalidOperationException("Order must be VerifiedOk.");
            Status = "SellerPaid";
            AddEvent(new SellerPaid(Id, SellerId, Price, payoutId));
        }

        private void AddEvent(object evt) => _events.Add(evt);
    }
}
