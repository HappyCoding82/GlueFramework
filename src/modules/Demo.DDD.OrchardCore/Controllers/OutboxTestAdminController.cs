using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlueFramework.Core.UOW;
using OrchardCore.Admin;
using System.Data;
using System.Data.Common;
using Demo.DDD.OrchardCore.Application;
using Demo.DDD.OrchardCore.Infrastructure.DbModels;

namespace Demo.DDD.OrchardCore.Controllers
{
    [Admin]
    public sealed class OutboxTestAdminController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IDemoOutboxTestAppService _svc;
        private readonly GlueFramework.Core.Abstractions.IDbConnectionAccessor _db;
        private readonly GlueFramework.Core.Abstractions.IDataTablePrefixProvider _prefix;

        public OutboxTestAdminController(
            IAuthorizationService authorizationService,
            IDemoOutboxTestAppService svc,
            GlueFramework.Core.Abstractions.IDbConnectionAccessor db,
            GlueFramework.Core.Abstractions.IDataTablePrefixProvider prefix)
        {
            _authorizationService = authorizationService;
            _svc = svc;
            _db = db;
            _prefix = prefix;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageDemoOutbox))
                return Forbid();

            var vm = await LoadAsync(cancellationToken);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Trigger(string? message, CancellationToken cancellationToken)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageDemoOutbox))
                return Forbid();

            var requestId = await _svc.TriggerAsync(message, cancellationToken);
            TempData["LastRequestId"] = requestId;
            return RedirectToAction(nameof(Index));
        }

        private async Task<OutboxTestVm> LoadAsync(CancellationToken cancellationToken)
        {
            var last = TempData["LastRequestId"] as string;

            var req = new List<OutboxTestRequestRow>();
            var handled = new List<OutboxTestHandledRow>();
            var outbox = new List<OutboxMessageRow>();

            await using var conn = _db.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                await ((DbConnection)conn).OpenAsync(cancellationToken);

            var requestRepo = new Repository<DemoOutboxTestRequestRecord>(conn, _prefix);
            var handledRepo = new Repository<DemoOutboxTestHandledRecord>(conn, _prefix);
            var outboxRepo = new Repository<OutboxMessageDbRecord>(conn, _prefix);

            var requestRows = await requestRepo.QueryTopAsync(x => x.Id >= 0, 20);
            req.AddRange(
                requestRows
                    .OrderByDescending(x => x.CreatedUtc)
                    .Select(x => new OutboxTestRequestRow(x.RequestId, x.Message, x.CreatedUtc)));

            var handledRows = await handledRepo.QueryTopAsync(x => x.Id >= 0, 20);
            handled.AddRange(
                handledRows
                    .OrderByDescending(x => x.HandledUtc)
                    .Select(x => new OutboxTestHandledRow(x.RequestId, x.Handler, x.HandledUtc)));

            var outboxRows = await outboxRepo.QueryTopAsync(x => x.Id >= 0, 50);
            outbox.AddRange(
                outboxRows
                    .OrderByDescending(x => x.CreatedUtc)
                    .Where(x => x.Type.Contains(nameof(Application.IntegrationEvents.DemoOutboxTestRequested), StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(x => new OutboxMessageRow(
                        x.MessageId,
                        x.Type,
                        x.Status,
                        x.TryCount,
                        x.CreatedUtc,
                        x.NextRetryUtc,
                        x.LastError)));

            return new OutboxTestVm(last, req, handled, outbox);
        }

        public sealed record OutboxTestVm(string? LastRequestId, List<OutboxTestRequestRow> Requests, List<OutboxTestHandledRow> Handled, List<OutboxMessageRow> OutboxMessages);
        public sealed record OutboxTestRequestRow(string RequestId, string? Message, DateTime CreatedUtc);
        public sealed record OutboxTestHandledRow(string RequestId, string? Handler, DateTime HandledUtc);
        public sealed record OutboxMessageRow(string MessageId, string Type, string Status, int TryCount, DateTime CreatedUtc, DateTime? NextRetryUtc, string? LastError);
    }
}
