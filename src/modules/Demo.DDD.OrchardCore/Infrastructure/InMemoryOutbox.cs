using System.Collections.Concurrent;

namespace Demo.DDD.OrchardCore.Infrastructure
{
    public sealed class InMemoryOutbox
    {
        private readonly ConcurrentQueue<(string Type, string Payload)> _items = new();

        public void Append(string type, string payload)
        {
            _items.Enqueue((type, payload));
        }

        public List<(string Type, string Payload)> Snapshot()
        {
            return _items.ToList();
        }
    }
}
