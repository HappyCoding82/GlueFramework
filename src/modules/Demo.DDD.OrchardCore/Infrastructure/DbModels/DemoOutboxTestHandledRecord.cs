using GlueFramework.Core.ORM;
using System.ComponentModel.DataAnnotations;

namespace Demo.DDD.OrchardCore.Infrastructure.DbModels
{
    [DataTable("Demo_OutboxTestHandled")]
    public sealed class DemoOutboxTestHandledRecord
    {
        [Key]
        [DBField("Id", true, true)]
        public int Id { get; set; }

        public string RequestId { get; set; } = default!;

        public string? Handler { get; set; }

        public DateTime HandledUtc { get; set; }
    }
}
