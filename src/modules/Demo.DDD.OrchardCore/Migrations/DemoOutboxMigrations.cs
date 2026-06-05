using Microsoft.Extensions.Logging;
using OrchardCore.Data.Migration;
using System.Threading.Tasks;

namespace Demo.DDD.OrchardCore.Migrations
{
    public sealed class DemoOutboxMigrations : DataMigration
    {
        private readonly ILogger<DemoOutboxMigrations> _logger;

        public DemoOutboxMigrations(ILogger<DemoOutboxMigrations> logger)
        {
            _logger = logger;
        }

        public async Task<int> CreateAsync()
        {
            _logger.LogInformation("Running DemoOutboxMigrations.CreateAsync");

            await SchemaBuilder.CreateTableAsync("Demo_OutboxTestRequest", table => table
                .Column<int>("Id", c => c.PrimaryKey().Identity())
                .Column<string>("RequestId", c => c.WithLength(64))
                .Column<string>("Message", c => c.WithLength(2048))
                .Column<System.DateTime>("CreatedUtc")
            );

            await SchemaBuilder.AlterTableAsync("Demo_OutboxTestRequest", table =>
            {
                table.CreateIndex("IDX_Demo_OutboxTestRequest_RequestId", "RequestId");
                table.CreateIndex("IDX_Demo_OutboxTestRequest_CreatedUtc", "CreatedUtc");
            });

            await SchemaBuilder.CreateTableAsync("Demo_OutboxTestHandled", table => table
                .Column<int>("Id", c => c.PrimaryKey().Identity())
                .Column<string>("RequestId", c => c.WithLength(64))
                .Column<string>("Handler", c => c.WithLength(512))
                .Column<System.DateTime>("HandledUtc")
            );

            await SchemaBuilder.AlterTableAsync("Demo_OutboxTestHandled", table =>
            {
                table.CreateIndex("IDX_Demo_OutboxTestHandled_RequestId", "RequestId");
                table.CreateIndex("IDX_Demo_OutboxTestHandled_Handler", "Handler");
                table.CreateIndex("IDX_Demo_OutboxTestHandled_HandledUtc", "HandledUtc");
            });

            return 1;
        }
    }
}
