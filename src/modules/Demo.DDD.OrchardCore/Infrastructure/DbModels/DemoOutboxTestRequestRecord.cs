using GlueFramework.Core.ORM;
using System.ComponentModel.DataAnnotations;

namespace Demo.DDD.OrchardCore.Infrastructure.DbModels
{
    [DataTable("Demo_OutboxTestRequest")]
    public sealed class DemoOutboxTestRequestRecord
    {
        [Key]
        [DBField("Id", true, true)]
        public int Id { get; set; }

        public string RequestId { get; set; } = default!;

        public string? Message { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
