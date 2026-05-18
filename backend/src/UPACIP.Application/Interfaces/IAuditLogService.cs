namespace UPACIP.Application.Interfaces;

/// <summary>
/// Writes immutable audit entries to the dedicated <c>audit.audit_log</c> table.
/// Implementations must use a database connection with INSERT-only privileges
/// on the audit schema (DR-003, NFR-007). EF Core is intentionally bypassed
/// to prevent recursive interceptor calls.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Appends an audit entry. The call must be non-destructive and idempotent
    /// with respect to the domain save — a failure here must not roll back the
    /// main transaction.
    /// </summary>
    Task LogAsync(
        Guid actorId,
        string actorRole,
        string actionType,
        string targetEntity,
        Guid targetId,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
