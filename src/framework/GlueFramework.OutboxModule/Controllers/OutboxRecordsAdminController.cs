using GlueFramework.OutboxModule.Services;
using GlueFramework.OutboxModule.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;

namespace GlueFramework.OutboxModule.Controllers
{
    [Admin]
    public sealed class OutboxRecordsAdminController : Controller
    {
        private readonly OutboxService _outbox;
        private readonly IAuthorizationService _authorizationService;

        public OutboxRecordsAdminController(OutboxService outbox, IAuthorizationService authorizationService)
        {
            _outbox = outbox;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index(int take = 50, string? status = null, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var vm = new OutboxRecordsIndexViewModel
            {
                Take = Math.Clamp(take, 1, 500),
                Status = status
            };

            vm.Items = await _outbox.QueryAsync(vm.Take, vm.Status, cancellationToken);
            return View(vm);
        }

        public async Task<IActionResult> Detail(Guid messageId, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            var item = await _outbox.GetAsync(messageId, cancellationToken);
            if (item == null)
                return NotFound();

            return View(new OutboxRecordDetailViewModel { Item = item });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid messageId, string status, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            await _outbox.UpdateStatusAsync(messageId, status, cancellationToken);
            return RedirectToAction(nameof(Detail), new { messageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(Guid messageId, CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOutbox))
                return Forbid();

            await _outbox.ArchiveAsync(messageId, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
    }
}
