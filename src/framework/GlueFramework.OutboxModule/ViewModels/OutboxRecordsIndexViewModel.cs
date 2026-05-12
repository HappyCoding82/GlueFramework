using System.Collections.Generic;
using GlueFramework.Core.Abstractions.Outbox;

namespace GlueFramework.OutboxModule.ViewModels
{
    public sealed class OutboxRecordsIndexViewModel
    {
        public int Take { get; set; }

        public string? Status { get; set; }

        public List<OutboxMessageRecord> Items { get; set; } = new();
    }
}
