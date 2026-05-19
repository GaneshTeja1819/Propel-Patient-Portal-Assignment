using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using UPACIP.Application.DTOs;
using UPACIP.Application.Handlers.Admin;
using UPACIP.Application.Interfaces;
using UPACIP.Application.Queries.Admin;

namespace UPACIP.API.Controllers;

/// <summary>
/// Admin-only read endpoints for the HIPAA audit log (SCR-017 / EP-007 / us_025).
/// All routes require <c>AdminPolicy</c> (role = "Admin" in JWT).
/// Only GET methods are exposed — HTTP 405 for all other verbs (AC-006).
/// All endpoints are marked <c>NoStore</c> to prevent PHI caching in proxies
/// (HIPAA §164.312(b)).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "AdminPolicy")]
[Route("api/v{version:apiVersion}/admin/audit-log")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAuditLogController : ControllerBase
{
    private readonly GetAuditLogHandler _getAuditLogHandler;
    private readonly GetAuditLogStatsHandler _getAuditLogStatsHandler;
    private readonly IAuditLogReadRepository _repository;
    private readonly IAuditLogService _auditLogService;

    public AdminAuditLogController(
        GetAuditLogHandler getAuditLogHandler,
        GetAuditLogStatsHandler getAuditLogStatsHandler,
        IAuditLogReadRepository repository,
        IAuditLogService auditLogService)
    {
        _getAuditLogHandler = getAuditLogHandler;
        _getAuditLogStatsHandler = getAuditLogStatsHandler;
        _repository = repository;
        _auditLogService = auditLogService;
    }

    // ── List (paginated) ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated page of audit events matching the supplied filters,
    /// ordered newest-first. Empty result (0 rows) returns HTTP 200 with
    /// <c>{ data: [], total: 0 }</c> — never HTTP 404 (AC-001 edge case).
    /// Writing <c>AUDIT_LOG_VIEWED</c> meta-event is fire-and-forget (AC-008).
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedResult<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? category,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateDateRange(from, to, out var dateError))
            return BadRequest(new { error = dateError });

        var query = new GetAuditLogQuery(
            DateFrom: from,
            DateTo: to,
            ActionCategory: category,
            Role: role,
            Status: status,
            SearchText: q,
            Page: page,
            PageSize: pageSize);

        var result = await _getAuditLogHandler.HandleAsync(ResolveActorId(), query, cancellationToken);
        return Ok(result);
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns today's (UTC) summary statistics:
    /// <c>EventsToday</c>, <c>UniqueUsers</c>, <c>PhiAccessEvents</c>, <c>FailedAttempts</c> (AC-001).
    /// </summary>
    [HttpGet("stats")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AuditLogStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _getAuditLogStatsHandler.HandleAsync(cancellationToken);
        return Ok(stats);
    }

    // ── CSV Export ────────────────────────────────────────────────────────────

    /// <summary>
    /// Streams filtered audit events as a CSV file (AC-005).
    /// Rows are streamed via <c>IAsyncEnumerable</c> to avoid loading all rows
    /// into memory (edge case: exports &gt; 10,000 rows).
    /// PHI field values are excluded from the CSV output (HIPAA §164.312(b)).
    /// Writing <c>AUDIT_LOG_EXPORTED</c> meta-event is fire-and-forget.
    /// Returns HTTP 200 with headers-only CSV when 0 rows match (edge case AC-005).
    /// </summary>
    [HttpGet("export")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task GetExport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? category,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (!ValidateDateRange(from, to, out var dateError))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = dateError }, cancellationToken);
            return;
        }

        var filename = $"audit_log_{DateTime.UtcNow:yyyy-MM-dd}.csv";
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";

        await using var writer = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);

        // CSV header row (no PHI column — PayloadJson excluded, AC-005).
        await writer.WriteLineAsync(
            "EventId,Timestamp,ActorRole,ActionType,ActionCategory,Resource,TargetId,IpAddress,Status,PhiAccess");

        var query = new GetAuditLogQuery(
            DateFrom: from,
            DateTo: to,
            ActionCategory: category,
            Role: role,
            Status: status,
            SearchText: q,
            Page: 1,
            PageSize: GetAuditLogQuery.MaxPageSize);

        await foreach (var entry in _repository.StreamAsync(query, cancellationToken))
        {
            await writer.WriteLineAsync(BuildCsvLine(entry));
        }

        await writer.FlushAsync(cancellationToken);

        // AC-005: write AUDIT_LOG_EXPORTED meta-event (fire-and-forget).
        _ = _auditLogService.LogAsync(
            actorId: ResolveActorId(),
            actorRole: "Admin",
            actionType: "AUDIT_LOG_EXPORTED",
            targetEntity: "audit_log",
            targetId: Guid.Empty,
            metadata: $"{{\"date\":\"{DateTime.UtcNow:yyyy-MM-dd}\",\"format\":\"csv\"}}",
            cancellationToken: CancellationToken.None);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid ResolveActorId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static bool ValidateDateRange(DateOnly? from, DateOnly? to, out string? error)
    {
        error = null;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            error = "DateFrom must not be later than DateTo.";
            return false;
        }
        return true;
    }

    private static string BuildCsvLine(AuditLogEntryDto entry)
    {
        return string.Join(',',
            CsvEscape(entry.EventId),
            CsvEscape(entry.Timestamp.ToString("o")),
            CsvEscape(entry.ActorRole),
            CsvEscape(entry.ActionType),
            CsvEscape(entry.ActionCategory),
            CsvEscape(entry.Resource),
            CsvEscape(entry.TargetId ?? string.Empty),
            CsvEscape(entry.IpAddress ?? string.Empty),
            CsvEscape(entry.Status),
            entry.PhiAccess ? "true" : "false");
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }
}
