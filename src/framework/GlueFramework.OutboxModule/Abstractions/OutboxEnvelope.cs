using System;

namespace Tierdrop.Outbox.Abstractions
{
    public sealed record OutboxEnvelope(Guid MessageId, string Type, string Payload, DateTimeOffset OccurredUtc);
}
