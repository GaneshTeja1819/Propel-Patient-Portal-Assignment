using UPACIP.Application.DTOs;
using UPACIP.Application.Queries.Admin;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Read-only access to <c>audit.audit_log</c> using the <c>audit_reader</c>
/// PostgreSQL role (SELECT only — no write path here).
/// </summary>
public interface IAuditLogReadRepository
{
    /// <summary>
    /// Returns a page of audit events matching the supplied filters, ordered newest-first.
    /// Returns HTTP-200-safe empty result when no rows match (AC-001 edge case).
    /// </summary>
    Task<PagedResult<AuditLogEntryDto>> GetPagedAsync(
        GetAuditLogQuery query,
        CancellationToken ct = default);

    /// <summary>Returns today's (UTC) summary statistics (AC-001).</summary>
    Task<AuditLogStatsDto> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Streams all events matching <paramref name="query"/> without loading all rows
    /// into memory — used by the CSV export endpoint (AC-005).
    /// </summary>
    IAsyncEnumerable<AuditLogEntryDto> StreamAsync(
        GetAuditLogQuery query,
        CancellationToken ct = default);
}
