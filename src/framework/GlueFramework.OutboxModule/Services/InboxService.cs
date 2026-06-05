using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.Core.Services;
using GlueFramework.Core.UOW;
using GlueFramework.OutboxModule.Infrastructure;

namespace GlueFramework.OutboxModule.Services
{
    public sealed class InboxService : ServiceBase, IInboxStore
    {
        private readonly IDALFactory _dalFactory;

        public InboxService(IDALFactory dalFactory, IDbConnectionAccessor dbConnAccessor, IDataTablePrefixProvider dataTablePrefixProvider)
            : base(dbConnAccessor, dataTablePrefixProvider)
        {
            _dalFactory = dalFactory;
        }

        public async Task<bool> TryBeginHandleAsync(Guid messageId, string handler, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlInboxStore>(dbSession);
            return await dal.TryBeginHandleAsync(messageId, handler, nowUtc, cancellationToken);
        }

        public async Task<int> CleanupAsync(DateTimeOffset olderThanUtc, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlInboxStore>(dbSession);
            return await dal.CleanupAsync(olderThanUtc, cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlInboxStore>(dbSession);
            return await dal.CountAsync(cancellationToken);
        }
    }
}
