namespace UPACIP.Application.DTOs;

/// <summary>
/// Read-only projection of a single <c>audit.audit_log</c> row.
/// Missing DB columns (ActorName, ActorEmail, IpAddress, UserAgent, CorrelationId, SessionId)
/// are extracted from the <c>metadata</c> JSON blob when present.
/// <c>PayloadJson</c> has PHI field values replaced with <c>[PHI-REDACTED]</c>
/// when <c>PhiAccess</c> is <see langword="true"/> (AC-004, HIPAA §164.312(b)).
/// </summary>
public sealed record AuditLogEntryDto(
    string EventId,
    string? CorrelationId,
    string? SessionId,
    DateTimeOffset Timestamp,
    string ActorId,
    string ActorName,
    string ActorEmail,
    string ActorRole,
    string? IpAddress,
    string? UserAgent,
    string ActionType,
    string ActionCategory,
    string Resource,
    string? TargetId,
    string? PayloadJson,
    string Status,
    bool PhiAccess
);
