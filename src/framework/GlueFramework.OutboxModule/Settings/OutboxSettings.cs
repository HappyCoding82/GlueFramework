namespace GlueFramework.OutboxModule.Settings
{
    public sealed class OutboxSettings
    {
        public bool Enabled { get; set; } = true;

        public bool AutoEnqueueIntegrationEvents { get; set; } = true;

        public int DispatchIntervalSeconds { get; set; } = 5;

        public int BatchSize { get; set; } = 50;

        public int InboxRetentionDays { get; set; } = 30;

        public bool EnableInboxCleanup { get; set; } = true;
    }
}
