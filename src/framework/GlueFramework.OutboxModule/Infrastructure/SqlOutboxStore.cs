using Dapper;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Diagnostics;
using GlueFramework.Core.ORM;
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
                LockToken = null,
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

        public async Task<List<OutboxMessageRecord>> GetTimedOutProcessingAsync(int batchSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            var repo = GetRepository<OutboxMessageDbModel>();
            var nowUtcDt = nowUtc.UtcDateTime;
            var rows = await repo.QueryTopAsync(x =>
                x.Status == "Processing"
                && x.LockedUntilUtc != null
                && x.LockedUntilUtc <= nowUtcDt,
                Math.Max(1, batchSize));

            return rows
                .OrderBy(x => x.OccurredUtc)
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
                .Set(x => x.LockedUntilUtc, (DateTime?)null)
                .Set(x => x.LockToken, (string?)null)
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
                LockToken = existing.LockToken,
                NextRetryUtc = existing.NextRetryUtc,
                LastError = existing.LastError,
                CreatedUtc = existing.CreatedUtc,
                UpdatedUtc = existing.UpdatedUtc,
                ArchivedUtc = nowUtc.UtcDateTime
            });

            await repo.DeleteAsync(existing);
            return true;
        }

        public async Task<string?> TryMarkProcessingAsync(Guid messageId, DateTimeOffset nowUtc, DateTimeOffset lockedUntilUtc, CancellationToken cancellationToken = default)
        {
            var lockToken = Guid.NewGuid().ToString("N");
            var sql = $@"
UPDATE {OutboxTableName}
SET {Column("Status")} = 'Processing',
    {Column("LockedUntilUtc")} = @LockedUntilUtc,
    {Column("LockToken")} = @LockToken,
    {Column("UpdatedUtc")} = @NowUtc
WHERE {Column("MessageId")} = @MessageId
  AND ({Column("Status")} = 'Pending'
       OR ({Column("Status")} = 'Failed'
           AND {Column("NextRetryUtc")} IS NOT NULL
           AND {Column("NextRetryUtc")} <= @NowUtc))
  AND ({Column("LockedUntilUtc")} IS NULL OR {Column("LockedUntilUtc")} <= @NowUtc);";

            var affected = await DbConnection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    MessageId = messageId.ToString("N"),
                    NowUtc = nowUtc.UtcDateTime,
                    LockedUntilUtc = lockedUntilUtc.UtcDateTime,
                    LockToken = lockToken
                },
                _session.Transaction,
                cancellationToken: cancellationToken));

            return affected > 0 ? lockToken : null;
        }

        public async Task<bool> MarkProcessingTimedOutAsync(Guid messageId, string? lockToken, DateTimeOffset nowUtc, int nextRetrySeconds, CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {OutboxTableName}
SET {Column("Status")} = 'Failed',
    {Column("TryCount")} = {Column("TryCount")} + 1,
    {Column("LockedUntilUtc")} = NULL,
    {Column("LockToken")} = NULL,
    {Column("NextRetryUtc")} = @NextRetryUtc,
    {Column("LastError")} = @LastError,
    {Column("UpdatedUtc")} = @NowUtc
WHERE {Column("MessageId")} = @MessageId
  AND {Column("Status")} = 'Processing'
  AND {Column("LockedUntilUtc")} IS NOT NULL
  AND {Column("LockedUntilUtc")} <= @NowUtc
  AND ({Column("LockToken")} = @LockToken OR ({Column("LockToken")} IS NULL AND @LockToken IS NULL));";

            var affected = await DbConnection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    MessageId = messageId.ToString("N"),
                    LockToken = lockToken,
                    NowUtc = nowUtc.UtcDateTime,
                    NextRetryUtc = nowUtc.AddSeconds(Math.Max(5, nextRetrySeconds)).UtcDateTime,
                    LastError = "Processing timeout"
                },
                _session.Transaction,
                cancellationToken: cancellationToken));

            return affected > 0;
        }

        public async Task<bool> MarkSucceededAsync(Guid messageId, string lockToken, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {OutboxTableName}
SET {Column("Status")} = 'Succeeded',
    {Column("LockedUntilUtc")} = NULL,
    {Column("LockToken")} = NULL,
    {Column("NextRetryUtc")} = NULL,
    {Column("LastError")} = NULL,
    {Column("UpdatedUtc")} = @NowUtc
WHERE {Column("MessageId")} = @MessageId
  AND {Column("Status")} = 'Processing'
  AND {Column("LockToken")} = @LockToken;";

            var affected = await DbConnection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    MessageId = messageId.ToString("N"),
                    LockToken = lockToken,
                    NowUtc = nowUtc.UtcDateTime
                },
                _session.Transaction,
                cancellationToken: cancellationToken));

            return affected > 0;
        }

        public async Task<bool> MarkFailedAsync(Guid messageId, string lockToken, DateTimeOffset nowUtc, string error, int nextRetrySeconds, CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {OutboxTableName}
SET {Column("Status")} = 'Failed',
    {Column("TryCount")} = {Column("TryCount")} + 1,
    {Column("LockedUntilUtc")} = NULL,
    {Column("LockToken")} = NULL,
    {Column("NextRetryUtc")} = @NextRetryUtc,
    {Column("LastError")} = @LastError,
    {Column("UpdatedUtc")} = @NowUtc
WHERE {Column("MessageId")} = @MessageId
  AND {Column("Status")} = 'Processing'
  AND {Column("LockToken")} = @LockToken;";

            var affected = await DbConnection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    MessageId = messageId.ToString("N"),
                    LockToken = lockToken,
                    NowUtc = nowUtc.UtcDateTime,
                    NextRetryUtc = nowUtc.AddSeconds(Math.Max(5, nextRetrySeconds)).UtcDateTime,
                    LastError = error
                },
                _session.Transaction,
                cancellationToken: cancellationToken));

            return affected > 0;
        }

        public async Task<bool> RetryNowAsync(Guid messageId, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {OutboxTableName}
SET {Column("Status")} = 'Pending',
    {Column("LockedUntilUtc")} = NULL,
    {Column("LockToken")} = NULL,
    {Column("NextRetryUtc")} = NULL,
    {Column("UpdatedUtc")} = @NowUtc
WHERE {Column("MessageId")} = @MessageId
  AND {Column("Status")} = 'Failed';";

            var affected = await DbConnection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    MessageId = messageId.ToString("N"),
                    NowUtc = nowUtc.UtcDateTime
                },
                _session.Transaction,
                cancellationToken: cancellationToken));

            return affected > 0;
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
            return new OutboxMessageRecord(messageId, m.Type, m.Payload, occurredUtc, m.Status, m.TryCount, lockedUntil, nextRetry, m.LastError, m.LockToken);
        }

        private string OutboxTableName
        {
            get
            {
                var tableName = (_tableNamePrefixProvider?.Prefix ?? string.Empty) + ModelName.Table<OutboxMessageDbModel>();
                var schema = (_tableNamePrefixProvider as ITenantTableSettingsProvider)?.Schema;
                return string.IsNullOrWhiteSpace(schema)
                    ? QuoteIdentifier(tableName)
                    : QuoteIdentifier(schema) + "." + QuoteIdentifier(tableName);
            }
        }

        private string Column(string name) => QuoteIdentifier(name);

        private string QuoteIdentifier(string name)
        {
            return GetIdentifierDelimiter() switch
            {
                ('"', '"') => $"\"{name.Replace("\"", "\"\"")}\"",
                ('`', '`') => $"`{name.Replace("`", "``")}`",
                _ => $"[{name.Replace("]", "]]" )}]"
            };
        }

        private (char Prefix, char Suffix) GetIdentifierDelimiter()
        {
            var connectionType = DbConnectionUnwrapper.Unwrap(DbConnection).GetType().FullName ?? string.Empty;
            if (connectionType.Contains("Npgsql", StringComparison.InvariantCultureIgnoreCase) ||
                connectionType.Contains("Postgre", StringComparison.InvariantCultureIgnoreCase))
                return ('"', '"');
            if (connectionType.Contains("MySql", StringComparison.InvariantCultureIgnoreCase))
                return ('`', '`');
            return ('[', ']');
        }
    }
}
