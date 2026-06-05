using Microsoft.Extensions.Logging;
using OrchardCore.Data.Migration;

namespace Demo.TxTestModule.Migrations;

public sealed class TxTestMigrations : DataMigration
{
    private readonly ILogger<TxTestMigrations> _logger;

    public TxTestMigrations(ILogger<TxTestMigrations> logger)
    {
        _logger = logger;
    }

    public async Task<int> CreateAsync()
    {
        _logger.LogInformation("Running TxTestMigrations.CreateAsync");

        await SchemaBuilder.CreateTableAsync("Demo_TxTestRecord", table => table
            .Column<int>("Id", c => c.PrimaryKey().Identity())
            .Column<string>("Name", c => c.WithLength(256))
            .Column<DateTime>("CreatedUtc")
        );

        await SchemaBuilder.AlterTableAsync("Demo_TxTestRecord", table =>
        {
            table.CreateIndex("IDX_Demo_TxTestRecord_Name", "Name");
            table.CreateIndex("IDX_Demo_TxTestRecord_CreatedUtc", "CreatedUtc");
        });

        return 1;
    }
}
