using GlueFramework.Core.ORM;
using System.ComponentModel.DataAnnotations;

namespace GlueFramework.OutboxModule.Infrastructure.DbModels
{
    [DataTable("InboxMessage")]
    public sealed class InboxMessageDbModel
    {
        [DBField("Id", false, true)]
        public int Id { get; set; }

        [Key]
        [DBField("MessageId", true, false)]
        public string MessageId { get; set; } = default!;

        [DBField("Handler", true, false)]
        public string Handler { get; set; } = default!;

        public DateTime ProcessedUtc { get; set; }
    }
}
