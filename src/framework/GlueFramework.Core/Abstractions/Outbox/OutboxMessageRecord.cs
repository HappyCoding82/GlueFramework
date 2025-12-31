using System;

namespace GlueFramework.Core.Abstractions.Outbox
{
    public sealed record OutboxMessageRecord(
        Guid MessageId,
        string Type,
        string Payload,
        DateTimeOffset OccurredUtc,
        string Status,
        int TryCount,
        DateTimeOffset? LockedUntilUtc,
        DateTimeOffset? NextRetryUtc,
        string? LastError
    );
}
