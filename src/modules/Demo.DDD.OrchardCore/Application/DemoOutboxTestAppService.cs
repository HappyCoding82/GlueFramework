using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using Demo.DDD.OrchardCore.Application.IntegrationEvents;
using Demo.DDD.OrchardCore.Infrastructure.DbModels;

namespace Demo.DDD.OrchardCore.Application
{
    public sealed class DemoOutboxTestAppService : ServiceBase, IDemoOutboxTestAppService
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<DemoOutboxTestAppService> _logger;

        public DemoOutboxTestAppService(
            IDbConnectionAccessor dbConnAccessor,
            IDataTablePrefixProvider dataTablePrefixProvider,
            IEventBus eventBus,
            ILogger<DemoOutboxTestAppService> logger)
            : base(dbConnAccessor, dataTablePrefixProvider)
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        [Transactional]
        public async Task<string> TriggerAsync(string? message, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var msg = string.IsNullOrWhiteSpace(message) ? $"Demo outbox trigger at {DateTime.UtcNow:O}" : message.Trim();

            using (var s = OpenJoinQuerySessionScope())
            {
                var repo = s.GetRepository<DemoOutboxTestRequestRecord>();
                await repo.InsertAsync(new DemoOutboxTestRequestRecord
                {
                    RequestId = requestId,
                    Message = msg,
                    CreatedUtc = DateTime.UtcNow
                });
            }

            await _eventBus.PublishAfterCommitAsync(new DemoOutboxTestRequested(requestId, msg), cancellationToken);

            _logger.LogInformation("Demo outbox test requested. RequestId={RequestId}", requestId);
            return requestId;
        }

    }
}
