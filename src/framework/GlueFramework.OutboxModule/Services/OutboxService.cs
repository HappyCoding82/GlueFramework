using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.Core.Services;
using GlueFramework.OutboxModule.Infrastructure;
using GlueFramework.OutboxModule.Infrastructure.DbModels;

namespace GlueFramework.OutboxModule.Services
{
    public class OutboxService : ServiceBase
    {
        private readonly IDALFactory _dalFactory;

        public OutboxService(IDALFactory dalFactory,IDbConnectionAccessor dbConnAccessor, IDataTablePrefixProvider dataTablePrefixProvider) : base(dbConnAccessor, dataTablePrefixProvider)
        {
            _dalFactory = dalFactory;
        }

        public async Task<List<OutboxMessageRecord>> QueryAsync(int take, string? status, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.QueryAsync(take, status, cancellationToken);
        }

        public async Task<OutboxMessageRecord?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.GetAsync(messageId, cancellationToken);
        }

        public async Task<List<OutboxMessageArchiveDbModel>> QueryArchiveAsync(int take, string? status, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.QueryArchiveAsync(take, status, cancellationToken);
        }

        public async Task<OutboxMessageArchiveDbModel?> GetArchiveAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.GetArchiveAsync(messageId, cancellationToken);
        }

        public async Task<Guid> EnqueueAsync(string type, string payload, DateTimeOffset occurredUtc, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.EnqueueAsync(type, payload, occurredUtc, cancellationToken);
        }

        public async Task<int> CleanupSucceededAsync(DateTimeOffset olderThanUtc, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.CleanupSucceededAsync(olderThanUtc, cancellationToken);
        }

        public async Task<bool> UpdateStatusAsync(Guid messageId, string status, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.UpdateStatusAsync(messageId, status, DateTimeOffset.UtcNow, cancellationToken);
        }

        public async Task<bool> ArchiveAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.ArchiveAsync(messageId, DateTimeOffset.UtcNow, cancellationToken);
        }
    }
}
