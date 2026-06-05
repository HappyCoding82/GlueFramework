using System;
using System.Linq;
using Demo.DDD.OrchardCore.Domain.Events;

namespace Demo.DDD.OrchardCore.Domain
{
    public sealed class Listing
    {
        private readonly List<object> _events = new();

        private Listing(string id, string sellerId, string title, decimal price, string imageRef)
        {
            Id = id;
            SellerId = sellerId;
            Title = title;
            Price = price;
            ImageRef = imageRef;
            Status = "Active";
        }

        public string Id { get; }

        public string SellerId { get; }

        public string Title { get; }

        public decimal Price { get; }

        public string ImageRef { get; }

        public string Status { get; private set; }

        public IReadOnlyList<object> DomainEvents => _events;

        public IReadOnlyList<object> DequeueEvents()
        {
            if (_events.Count == 0)
                return Array.Empty<object>();

            var copy = _events.ToArray();
            _events.Clear();
            return copy;
        }

        public static Listing Create(string sellerId, string title, decimal price, string imageRef)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) throw new ArgumentException("SellerId required", nameof(sellerId));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required", nameof(title));
            if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));
            if (string.IsNullOrWhiteSpace(imageRef)) throw new ArgumentException("ListingImageRef required", nameof(imageRef));

            var id = "L-" + Guid.NewGuid().ToString("N");
            var listing = new Listing(id, sellerId, title, price, imageRef);
            listing.AddEvent(new ListingCreated(id, sellerId, title, price));
            return listing;
        }

        public void MarkSold()
        {
            if (Status != "Active") throw new InvalidOperationException("Listing is not active.");
            Status = "Sold";
        }

        private void AddEvent(object evt) => _events.Add(evt);
    }
}
