# Glue Framework Outbox Module

Reliable event publishing with Outbox and Inbox patterns for OrchardCore.

## Features

### Outbox Pattern

- **Reliable Event Publishing** - Events are persisted before dispatch
- **Automatic Retry** - Failed dispatches are retried with exponential backoff
- **Admin UI** - View, retry, and manage outbox messages
- **Auto-Cleanup** - Automatic cleanup of succeeded messages

### Inbox Pattern

- **Idempotent Handlers** - Per-handler deduplication to prevent duplicate processing
- **Handler-level Tracking** - Tracks each handler's execution status independently

## Admin Endpoints

- `GET /Admin/Outbox/Records` - View pending/failed messages
- `GET /Admin/Outbox/Archive` - View archived messages
- `POST /Admin/OutboxAdmin/UpdateOutboxStatus` - Manually update message status
- `POST /Admin/OutboxAdmin/ArchiveOutbox` - Archive a message

## Configuration

```yaml
Outbox:
  Enabled: true
  DispatchIntervalSeconds: 5
  BatchSize: 50
  InboxRetentionDays: 30
  EnableInboxCleanup: true
```

## Usage

```csharp
// Publishing integration events (automatic via decorator)
await _eventBus.PublishAsync(new OrderCreatedEvent { OrderId = order.Id });

// The event is stored in Outbox and dispatched reliably
```

## Architecture

- `OutboxDispatchService` - Background dispatch loop
- `InProcEventBus` - In-process event bus with inbox checking
- `IInboxStore` / `IOutboxStore` - Storage abstractions

Depends on `GlueFramework.OrchardCoreModule`.
