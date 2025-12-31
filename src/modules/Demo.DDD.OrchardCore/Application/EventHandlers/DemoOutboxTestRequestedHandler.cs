using GlueFramework.Core.Abstractions;
using GlueFramework.Core.UOW;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.Common;
using Demo.DDD.OrchardCore.Application.IntegrationEvents;
using Demo.DDD.OrchardCore.Infrastructure.DbModels;

namespace Demo.DDD.OrchardCore.Application.EventHandlers
{
    public sealed class DemoOutboxTestRequestedHandler : IEventHandler<DemoOutboxTestRequested>
    {
        private readonly IDbConnectionAccessor _db;
        private readonly IDataTablePrefixProvider _prefix;
        private readonly ILogger<DemoOutboxTestRequestedHandler> _logger;

        public DemoOutboxTestRequestedHandler(
            IDbConnectionAccessor db,
            IDataTablePrefixProvider prefix,
            ILogger<DemoOutboxTestRequestedHandler> logger)
        {
            _db = db;
            _prefix = prefix;
            _logger = logger;
        }

        public async Task HandleAsync(DemoOutboxTestRequested evt, CancellationToken cancellationToken = default)
        {
            await using var conn = _db.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                await ((DbConnection)conn).OpenAsync(cancellationToken);

            var repo = new Repository<DemoOutboxTestHandledRecord>(conn, _prefix);
            await repo.InsertAsync(new DemoOutboxTestHandledRecord
            {
                RequestId = evt.RequestId,
                Handler = nameof(DemoOutboxTestRequestedHandler),
                HandledUtc = DateTime.UtcNow
            });

            _logger.LogInformation("Demo outbox test handled. RequestId={RequestId} Message={Message}", evt.RequestId, evt.Message);
        }
    }
}
