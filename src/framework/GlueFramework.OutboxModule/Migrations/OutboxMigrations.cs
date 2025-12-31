using OrchardCore.Data.Migration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GlueFramework.OutboxModule.Migrations
{
    public sealed class OutboxMigrations : DataMigration
    {
        private readonly ILogger<OutboxMigrations> _logger;

        public OutboxMigrations(ILogger<OutboxMigrations> logger)
        {
            _logger = logger;
        }

        public async Task<int> CreateAsync()
        {
            _logger.LogInformation("Running OutboxMigrations.CreateAsync");

            await SchemaBuilder.CreateTableAsync("OutboxMessage", table => table
                .Column<int>("Id", c => c.PrimaryKey().Identity())
                .Column<string>("MessageId", c => c.WithLength(64))
                .Column<string>("Type", c => c.WithLength(512))
                .Column<string>("Payload", c => c.Unlimited())
                .Column<System.DateTime>("OccurredUtc")
                .Column<string>("Status", c => c.WithLength(32))
                .Column<int>("TryCount")
                .Column<System.DateTime>("LockedUntilUtc", c => c.Nullable())
                .Column<System.DateTime>("NextRetryUtc", c => c.Nullable())
                .Column<string>("LastError", c => c.Unlimited())
                .Column<System.DateTime>("CreatedUtc")
                .Column<System.DateTime>("UpdatedUtc")
            );

            await SchemaBuilder.AlterTableAsync("OutboxMessage", table =>
            {
                table.CreateIndex("IDX_OutboxMessage_Status", "Status");
                table.CreateIndex("IDX_OutboxMessage_NextRetryUtc", "NextRetryUtc");
                table.CreateIndex("IDX_OutboxMessage_CreatedUtc", "CreatedUtc");
                table.CreateIndex("IDX_OutboxMessage_MessageId", "MessageId");
            });

            await SchemaBuilder.CreateTableAsync("InboxMessage", table => table
                .Column<int>("Id", c => c.PrimaryKey().Identity())
                .Column<string>("MessageId", c => c.WithLength(64))
                .Column<string>("Handler", c => c.WithLength(512))
                .Column<System.DateTime>("ProcessedUtc")
            );

            await SchemaBuilder.AlterTableAsync("InboxMessage", table =>
            {
                table.CreateIndex("IDX_InboxMessage_MessageId", "MessageId");
                table.CreateIndex("IDX_InboxMessage_Handler", "Handler");
                table.CreateIndex("IDX_InboxMessage_ProcessedUtc", "ProcessedUtc");
            });

            return 1;
        }
    }
}
