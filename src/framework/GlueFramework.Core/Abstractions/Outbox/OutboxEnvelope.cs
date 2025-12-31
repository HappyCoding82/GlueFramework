using System;

namespace GlueFramework.Core.Abstractions.Outbox
{
    public sealed record OutboxEnvelope(Guid MessageId, string Type, string Payload, DateTimeOffset OccurredUtc);
}
