Framework.Outbox module

Admin endpoints:
- GET /Admin/OutboxAdmin/Outbox?take=50&status=Pending
- GET /Admin/OutboxAdmin/InboxCount
- GET /Admin/OutboxAdmin/Settings
- POST /Admin/OutboxAdmin/Settings (json body OutboxSettings)
- POST /Admin/OutboxAdmin/CleanupInbox?retentionDays=30

Appsettings (global defaults):
Outbox:
  Enabled: true
  DispatchIntervalSeconds: 5
  BatchSize: 50
  InboxRetentionDays: 30
  EnableInboxCleanup: true
