using UPACIP.Application.DTOs;
using UPACIP.Application.Interfaces;
using UPACIP.Application.Queries.Admin;

namespace UPACIP.Application.Handlers.Admin;

/// <summary>
/// Retrieves a filtered, paginated page of audit events and writes an
/// <c>AUDIT_LOG_VIEWED</c> meta-event for every list request (AC-008).
/// The meta-event write is fire-and-forget — a write failure does not
/// abort the read response (HIPAA: availability &gt; audit perfection).
/// </summary>
public sealed class GetAuditLogHandler
{
    private readonly IAuditLogReadRepository _repository;
    private readonly IAuditLogService _auditLogService;

    public GetAuditLogHandler(
        IAuditLogReadRepository repository,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<AuditLogEntryDto>> HandleAsync(
        Guid actorId,
        GetAuditLogQuery query,
        CancellationToken ct = default)
    {
        // Cap page size to prevent runaway memory allocation.
        var safeSize = Math.Clamp(query.PageSize, 1, GetAuditLogQuery.MaxPageSize);
        var safePage = Math.Max(query.Page, 1);
        var capped = query with { Page = safePage, PageSize = safeSize };

        var result = await _repository.GetPagedAsync(capped, ct);

        // AC-008: write AUDIT_LOG_VIEWED fire-and-forget (separate cancellation token).
        _ = _auditLogService.LogAsync(
            actorId, "Admin", "AUDIT_LOG_VIEWED", "audit_log", Guid.Empty,
            cancellationToken: CancellationToken.None);

        return result;
    }
}
