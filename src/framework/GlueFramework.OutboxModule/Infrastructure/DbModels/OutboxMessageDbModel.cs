using GlueFramework.Core.ORM;
using System.ComponentModel.DataAnnotations;

namespace GlueFramework.OutboxModule.Infrastructure.DbModels
{
    [DataTable("OutboxMessage")]
    public sealed class OutboxMessageDbModel
    {
        [DBField("Id", false, true)]
        public int Id { get; set; }

        [Key]
        [DBField("MessageId", true, false)]
        public string MessageId { get; set; } = default!;

        public string Type { get; set; } = default!;

        public string Payload { get; set; } = default!;

        public DateTime OccurredUtc { get; set; }

        public string Status { get; set; } = default!;

        public int TryCount { get; set; }

        public DateTime? LockedUntilUtc { get; set; }

        public DateTime? NextRetryUtc { get; set; }

        public string? LastError { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }
    }
}
