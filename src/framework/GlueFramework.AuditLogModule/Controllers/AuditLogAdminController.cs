using GlueFramework.AuditLogModule.Models;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.UOW;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrchardCore.Admin;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace GlueFramework.AuditLogModule.Controllers
{
    [Admin]
    public sealed class AuditLogAdminController : Controller
    {
        private readonly IDbConnectionAccessor _dbConnectionAccessor;
        private readonly IDataTablePrefixProvider _tablePrefixProvider;
        private readonly IAuthorizationService _authorizationService;

        public AuditLogAdminController(
            IDbConnectionAccessor dbConnectionAccessor,
            IDataTablePrefixProvider tablePrefixProvider,
            IAuthorizationService authorizationService)
        {
            _dbConnectionAccessor = dbConnectionAccessor;
            _tablePrefixProvider = tablePrefixProvider;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAuditLogs))
                return Forbid();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> List(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? actionContains,
            string? userContains,
            string? correlationId,
            bool? success,
            string? sortBy,
            bool desc = true,
            int pageIndex = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAuditLogs))
                return Forbid();

            await using var conn = _dbConnectionAccessor.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                await ((DbConnection)conn).OpenAsync(cancellationToken);

            var repo = new Repository<AuditLogRecord>(conn, _tablePrefixProvider);

            // NOTE: Do NOT use `x => true` here. The ORM's expression translator may turn it into `WHERE @1`,
            // which is invalid SQL on MSSQL. Use a safe always-true predicate.
            Expression<Func<AuditLogRecord, bool>> where = x => x.Id >= 0;

            if (fromUtc.HasValue)
            {
                var v = fromUtc.Value;
                where = And(where, x => x.CreatedUtc >= v);
            }

            if (toUtc.HasValue)
            {
                var v = toUtc.Value;
                where = And(where, x => x.CreatedUtc <= v);
            }

            if (!string.IsNullOrWhiteSpace(actionContains))
            {
                var v = actionContains.Trim();
                where = And(where, x => x.ActionName != null && x.ActionName.Contains(v));
            }

            if (!string.IsNullOrWhiteSpace(userContains))
            {
                var v = userContains.Trim();
                where = And(where, x => x.UserName != null && x.UserName.Contains(v));
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                var v = correlationId.Trim();
                where = And(where, x => x.CorrelationId == v);
            }

            if (success.HasValue)
            {
                var v = success.Value;
                where = And(where, x => x.Success == v);
            }

            var safeSort = NormalizeSort(sortBy);
            var safeSize = Math.Clamp(pageSize, 1, 200);
            var pager = new PagerInfo { PageIndex = Math.Max(1, pageIndex), PageSize = safeSize };

            // IMPORTANT: SqlBuilder_Base.BuildQuery(FilterOptions<T>) expects a full ORDER BY clause here
            // because it injects it into ROW_NUMBER() OVER({orderby}).
            var orderPart = $"ORDER BY {safeSort} {(desc ? "desc" : "asc")}";
            var filter = new FilterOptions<AuditLogRecord>(where, pager, new List<string> { orderPart });

            var result = await repo.PagerSearchAsync(filter);

            return Json(new
            {
                total = result.TotalCount,
                pageIndex = pager.PageIndex,
                pageSize = pager.PageSize,
                items = result.Results.Select(x => new
                {
                    x.Id,
                    x.CreatedUtc,
                    Action = x.ActionName,
                    x.Tenant,
                    User = x.UserName,
                    x.Success,
                    x.ElapsedMs,
                    x.TraceId,
                    x.SpanId,
                    x.CorrelationId
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupBefore(DateTime cutoffUtc, CancellationToken cancellationToken)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAuditLogs))
                return Forbid();

            await using var conn = _dbConnectionAccessor.CreateConnection();
            if (conn.State == ConnectionState.Closed)
                await ((DbConnection)conn).OpenAsync(cancellationToken);

            var repo = new Repository<AuditLogRecord>(conn, _tablePrefixProvider);
            await repo.DeleteAsync(x => x.CreatedUtc < cutoffUtc);

            return Ok(new { ok = true });
        }

        private static string NormalizeSort(string? sortBy)
        {
            // keep a whitelist to avoid SQL injection because order by is string based
            return sortBy?.Trim() switch
            {
                "CreatedUtc" => "CreatedUtc",
                "ElapsedMs" => "ElapsedMs",
                "Success" => "Success",
                "CorrelationId" => "CorrelationId",
                _ => "CreatedUtc"
            };
        }

        private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var p = Expression.Parameter(typeof(T), "x");
            var body = Expression.AndAlso(Expression.Invoke(left, p), Expression.Invoke(right, p));
            return Expression.Lambda<Func<T, bool>>(body, p);
        }
    }
}
