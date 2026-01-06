using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Abstractions.Outbox;
using GlueFramework.Core.Services;
using GlueFramework.OutboxModule.Infrastructure;

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

        public async Task<Guid> EnqueueAsync(string type, string payload, DateTimeOffset occurredUtc, CancellationToken cancellationToken = default)
        {
            using var dbSession = OpenDbSessionScope();
            var dal = _dalFactory.CreateDAL<SqlOutboxStore>(dbSession);
            return await dal.EnqueueAsync(type, payload, occurredUtc, cancellationToken);
        }
    }
}
