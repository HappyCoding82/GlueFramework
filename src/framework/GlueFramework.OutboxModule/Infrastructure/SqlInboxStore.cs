using GlueFramework.Core.Abstractions;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.Core.UOW;
using GlueFramework.OutboxModule.Infrastructure.DbModels;
using GlueFramework.Core.DataLayer;

namespace GlueFramework.OutboxModule.Infrastructure
{
    public sealed class SqlInboxStore : DALBase, IInboxStore
    {

        public SqlInboxStore(IDbSession dbSession, IDataTablePrefixProvider provider): base(dbSession, provider)
        {
        
        }

        public async Task<bool> TryBeginHandleAsync(Guid messageId, string handler, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            // Best-effort idempotency without a unique index.
            // For production, consider adding a unique index on (MessageId, Handler).
           

            var repo = GetRepository<InboxMessageDbModel>();
            var msgId = messageId.ToString("N");

            // NOTE: Query uses a best-effort check then insert.
            // Without a unique index, concurrent handlers may still insert duplicates.
            var existing = await repo.FirstOrDefaultAsync(x => x.MessageId == msgId && x.Handler == handler);
            if (existing != null)
                return false;

            await repo.InsertAsync(new InboxMessageDbModel
            {
                MessageId = msgId,
                Handler = handler,
                ProcessedUtc = nowUtc.UtcDateTime
            });

            return true;
        }

        public async Task<int> CleanupAsync(DateTimeOffset olderThanUtc, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<InboxMessageDbModel>();
            var olderThanUtcDt = olderThanUtc.UtcDateTime;
            var rows = await repo.QueryAsync(x => x.ProcessedUtc < olderThanUtcDt);
            var list = rows.ToList();
            if (list.Count == 0)
                return 0;

            await repo.DeleteAsync(list);
            return list.Count;
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<InboxMessageDbModel>();
            var all = await repo.QueryAsync(x => x.Id >= 0);
            return all.Count();
        }
    }
}
