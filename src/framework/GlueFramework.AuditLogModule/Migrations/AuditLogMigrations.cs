using OrchardCore.Data.Migration;

namespace GlueFramework.AuditLogModule.Migrations
{
    public sealed class AuditLogMigrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateTable("AuditLog", table => table
                .Column<int>("Id", c => c.PrimaryKey().Identity())
                .Column<string>("ActionName", c => c.WithLength(256))
                .Column<string>("Tenant", c => c.WithLength(256))
                .Column<string>("UserName", c => c.WithLength(256))
                .Column<bool>("Success")
                .Column<int>("ElapsedMs")
                .Column<string>("TraceId", c => c.WithLength(64))
                .Column<string>("SpanId", c => c.WithLength(32))
                .Column<string>("CorrelationId", c => c.WithLength(128))
                .Column<string>("ArgsJson", c => c.Unlimited())
                .Column<string>("ResultJson", c => c.Unlimited())
                .Column<string>("Exception", c => c.Unlimited())
                .Column<System.DateTime>("CreatedUtc")
            );

            SchemaBuilder.AlterTable("AuditLog", table =>
            {
                table.CreateIndex("IDX_AuditLog_CreatedUtc", "CreatedUtc");
                table.CreateIndex("IDX_AuditLog_ActionName", "ActionName");
                table.CreateIndex("IDX_AuditLog_CorrelationId", "CorrelationId");
            });

            return 1;
        }
    }
}
