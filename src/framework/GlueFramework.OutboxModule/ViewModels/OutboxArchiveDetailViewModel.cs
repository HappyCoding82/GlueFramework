using GlueFramework.OutboxModule.Infrastructure.DbModels;

namespace GlueFramework.OutboxModule.ViewModels
{
    public sealed class OutboxArchiveDetailViewModel
    {
        public OutboxMessageArchiveDbModel Item { get; set; } = default!;
    }
}
