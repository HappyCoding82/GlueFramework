using GlueFramework.Core.Abstractions;
using System;
using System.Collections.Generic;
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
    public sealed class SqlOutboxStore : DALBase , IOutboxStore
    {


        public SqlOutboxStore(IDbSession dbSession, IDataTablePrefixProvider provider) : base(dbSession, provider)
        {
        
        }

        public async Task<Guid> EnqueueAsync(string type, string payload, DateTimeOffset occurredUtc, CancellationToken cancellationToken = default)
        {
            var messageId = Guid.NewGuid();
          

            var repo = GetRepository<OutboxMessageDbModel>();
            await repo.InsertAsync(new OutboxMessageDbModel
            {
                MessageId = messageId.ToString("N"),
                Type = type,
                Payload = payload,
                OccurredUtc = occurredUtc.UtcDateTime,
                Status = "Pending",
                TryCount = 0,
                LockedUntilUtc = null,
                NextRetryUtc = null,
                LastError = null,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            });
            return messageId;
        }

        public async Task<List<OutboxMessageRecord>> GetPendingAsync(int batchSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            
            var repo = GetRepository<OutboxMessageDbModel>();
            var nowUtcDt = nowUtc.UtcDateTime;
            var rows = await repo.QueryAsync(x =>
                x.Status == "Pending"
                || (x.Status == "Failed" && x.NextRetryUtc != null && x.NextRetryUtc <= nowUtcDt));

            return rows
                .OrderBy(x => x.OccurredUtc)
                .Take(Math.Max(1, batchSize))
                .Select(Map)
                .ToList();
        }

        public async Task<OutboxMessageRecord?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<OutboxMessageDbModel>();
            var key = new OutboxMessageDbModel { MessageId = messageId.ToString("N") };
            var existing = await repo.GetSingleOrDefaultByKeyAsync(key);
            return existing == null ? null : Map(existing);
        }

        public async Task<List<OutboxMessageArchiveDbModel>> QueryArchiveAsync(int take, string? status, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<OutboxMessageArchiveDbModel>();
            IEnumerable<OutboxMessageArchiveDbModel> rows;
            if (string.IsNullOrWhiteSpace(status))
                rows = await repo.QueryAsync(x => x.Id >= 0);
            else
                rows = await repo.QueryAsync(x => x.Status == status);

            return rows
                .OrderByDescending(x => x.ArchivedUtc)
                .Take(Math.Max(1, take))
                .ToList();
        }

        public async Task<OutboxMessageArchiveDbModel?> GetArchiveAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<OutboxMessageArchiveDbModel>();
            var key = new OutboxMessageArchiveDbModel { MessageId = messageId.ToString("N") };
            return await repo.GetSingleOrDefaultByKeyAsync(key);
        }

        public async Task<int> CleanupSucceededAsync(DateTimeOffset olderThanUtc, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<OutboxMessageDbModel>();
            var olderThanUtcDt = olderThanUtc.UtcDateTime;
            var rows = await repo.QueryAsync(x => x.Status == "Succeeded" && x.UpdatedUtc < olderThanUtcDt);
            var list = rows.ToList();
            if (list.Count == 0)
                return 0;

            await repo.DeleteAsync(list);
            return list.Count;
        }

        public async Task<bool> UpdateStatusAsync(Guid messageId, string status, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            var repo = GetRepository<OutboxMessageDbModel>();
            var key = new OutboxMessageDbModel { MessageId = messageId.ToString("N") };
            var existing = await repo.GetSingleOrDefaultByKeyAsync(key);
            if (existing == null)
                return false;

            var affected = await repo.UpdatePartialAsync(key, p => p
                .Set(x => x.Status, status)
                .Set(x => x.UpdatedUtc, nowUtc.UtcDateTime));
            return affected > 0;
        }

        public async Task<bool> ArchiveAsync(Guid messageId, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<OutboxMessageDbModel>();
            var archiveRepo = GetRepository<OutboxMessageArchiveDbModel>();

            var key = new OutboxMessageDbModel { MessageId = messageId.ToString("N") };
            var existing = await repo.GetSingleOrDefaultByKeyAsync(key);
            if (existing == null)
                return false;

            await archiveRepo.InsertAsync(new OutboxMessageArchiveDbModel
            {
                MessageId = existing.MessageId,
                Type = existing.Type,
                Payload = existing.Payload,
                OccurredUtc = existing.OccurredUtc,
                Status = existing.Status,
                TryCount = existing.TryCount,
                LockedUntilUtc = existing.LockedUntilUtc,
                NextRetryUtc = existing.NextRetryUtc,
                LastError = existing.LastError,
                CreatedUtc = existing.CreatedUtc,
                UpdatedUtc = existing.UpdatedUtc,
                ArchivedUtc = nowUtc.UtcDateTime
            });

            await repo.DeleteAsync(existing);
            return true;
        }

        public async Task<bool> TryMarkProcessingAsync(Guid messageId, DateTimeOffset nowUtc, DateTimeOffset lockedUntilUtc, CancellationToken cancellationToken = default)
        {
            //await using var conn = _db.CreateConnection();
            //await conn.OpenAsync(cancellationToken);

            var repo = GetRepository<OutboxMessageDbModel>();
            var key = new OutboxMessageDbModel { MessageId = messageId.ToString("N") };
            var existing = await repo.GetSingleOrDefaultByKeyAsync(key);
            if (existing == null)
                return false;

            var nowUtcDt = nowUtc.UtcDateTime;
            var lockedUntilDt = lockedUntilUtc.UtcDateTime;

            var canClaim =
                (existing.Status == "Pending" || (existing.Status == "Failed" && existing.NextRetryUtc != null && existing.NextRetryUtc <= nowUtcDt))
                && (existing.LockedUntilUtc == null || existing.LockedUntilUtc < nowUtcDt);

            if (!canClaim)
                return false;

            var affected = await repo.UpdatePartialAsync(key, p => p
                .Set(x => x.Status, "Processing")
                .Set(x => x.LockedUntilUtc, lockedUntilDt)
                .Set(x => x.UpdatedUtc, nowUtcDt));

            return affected > 0;
        }

        public async Task MarkSucceededAsync(Guid messageId, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            
            var repo = GetRepository<OutboxMessageDbModel>();
            var nowUtcDt = nowUtc.UtcDateTime;
            var key = new OutboxMessageDbModel { MessageId = messageId.ToString("N") };
            await repo.UpdatePartialAsync(key, p => p
                .Set(x => x.Status, "Succeeded")
                .Set(x => x.LockedUntilUtc, (DateTime?)null)
                .Set(x => x.NextRetryUtc, (DateTime?)null)
                .Set(x => x.LastError, (string?)null)
                .Set(x => x.UpdatedUtc, nowUtcDt));
        }

        public async Task MarkFailedAsync(Guid messageId, DateTimeOffset nowUtc, string error, int nextRetrySeconds, CancellationToken cancellationToken = default)
        {
            var nextRetry = nowUtc.AddSeconds(Math.Max(5, nextRetrySeconds));
            var nowUtcDt = nowUtc.UtcDateTime;
            var nextRetryDt = nextRetry.UtcDateTime;

            var repo = GetRepository<OutboxMessageDbModel>();
            var key = new OutboxMessageDbModel { MessageId = messageId.ToString("N") };
            var existing = await repo.GetSingleOrDefaultByKeyAsync(key);
            var nextTry = (existing?.TryCount ?? 0) + 1;

            await repo.UpdatePartialAsync(key, p => p
                .Set(x => x.Status, "Failed")
                .Set(x => x.TryCount, nextTry)
                .Set(x => x.LockedUntilUtc, (DateTime?)null)
                .Set(x => x.NextRetryUtc, nextRetryDt)
                .Set(x => x.LastError, error)
                .Set(x => x.UpdatedUtc, nowUtcDt));
        }

        public async Task<List<OutboxMessageRecord>> QueryAsync(int take, string? status, CancellationToken cancellationToken = default)
        {
          
            var repo = GetRepository<OutboxMessageDbModel>();
            IEnumerable<OutboxMessageDbModel> rows;
            if (string.IsNullOrWhiteSpace(status))
                rows = await repo.QueryAsync(x => x.Id >= 0);
            else
                rows = await repo.QueryAsync(x => x.Status == status);

            return rows
                .OrderByDescending(x => x.OccurredUtc)
                .Take(Math.Max(1, take))
                .Select(Map)
                .ToList();
        }

        private static OutboxMessageRecord Map(OutboxMessageDbModel m)
        {
            var messageId = Guid.ParseExact(m.MessageId, "N");
            var occurredUtc = new DateTimeOffset(m.OccurredUtc, TimeSpan.Zero);
            DateTimeOffset? lockedUntil = m.LockedUntilUtc == null ? null : new DateTimeOffset(m.LockedUntilUtc.Value, TimeSpan.Zero);
            DateTimeOffset? nextRetry = m.NextRetryUtc == null ? null : new DateTimeOffset(m.NextRetryUtc.Value, TimeSpan.Zero);
            return new OutboxMessageRecord(messageId, m.Type, m.Payload, occurredUtc, m.Status, m.TryCount, lockedUntil, nextRetry, m.LastError);
        }
    }
}
