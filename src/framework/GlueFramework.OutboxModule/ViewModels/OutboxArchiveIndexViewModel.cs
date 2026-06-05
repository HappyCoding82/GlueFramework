using System.Collections.Generic;
using GlueFramework.OutboxModule.Infrastructure.DbModels;

namespace GlueFramework.OutboxModule.ViewModels
{
    public sealed class OutboxArchiveIndexViewModel
    {
        public int Take { get; set; }

        public string? Status { get; set; }

        public List<OutboxMessageArchiveDbModel> Items { get; set; } = new();
    }
}
