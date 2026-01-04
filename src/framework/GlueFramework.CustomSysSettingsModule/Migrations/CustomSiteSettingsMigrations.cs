using Microsoft.Extensions.Logging;
using YesSql.Sql;
using OrchardCore.Data.Migration;

namespace GlueFramework.CustomSysSettingsModule.Migrations
{
    public class CustomSiteSettingsMigrations : DataMigration
    {
        private readonly ILogger<CustomSiteSettingsMigrations> _logger;

        public CustomSiteSettingsMigrations(ILogger<CustomSiteSettingsMigrations> logger)
        {
            _logger = logger;
        }

        public async Task<int> CreateAsync()
        {
            _logger.LogInformation("Creating CustomSiteSettings table");

            // Create table
            await SchemaBuilder.CreateTableAsync("CustomSiteSettings", table => table
                .Column<int>("Id", c => c.PrimaryKey().Identity())
                .Column<string>("SKey", c => c.NotNull().WithLength(100))
                .Column<string>("SValue", c => c.Unlimited())
                .Column<string>("Group", c => c.WithLength(255))
                .Column<bool>("ReadOnly", c => c.WithDefault(false))
                .Column<bool>("DefaultVisible", c => c.WithDefault(true))
                .Column<bool>("Removable", c => c.WithDefault(true))
                .Column<string>("CreatedBy", c => c.WithLength(50))
                .Column<string>("LastModifiedBy", c => c.WithLength(50))
                .Column<DateTime>("CreatedDate")
                .Column<DateTime>("LastModifiedDate")
            );

            // Create Indexes
            await SchemaBuilder.AlterTableAsync("CustomSiteSettings", table =>
            {
                // Add SKey as Unique index
                // table.CreateIndex("IDX_CustomSiteSettings_SKey", "SKey");

                // add index on Group
                table.CreateIndex("IDX_CustomSiteSettings_Group", "Group");

                // Add ReadOnly, DefaultVisible indexes
                table.CreateIndex("IDX_CustomSiteSettings_ReadOnly", "ReadOnly");
                table.CreateIndex("IDX_CustomSiteSettings_DefaultVisible", "DefaultVisible");

                // Create group indexes
                table.CreateIndex("IDX_CustomSiteSettings_Group_ReadOnly", "Group", "ReadOnly");
            });

            _logger.LogInformation("CustomSiteSettings table created successfully");

            return 1;
        }

    }
}
