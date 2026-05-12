using GlueFramework.Core.Abstractions.Outbox;

namespace GlueFramework.OutboxModule.ViewModels
{
    public sealed class OutboxRecordDetailViewModel
    {
        public OutboxMessageRecord Item { get; set; } = default!;
    }
}
