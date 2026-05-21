namespace UPACIP.Application.DTOs;

/// <summary>Summary statistics for today's audit events (AC-001).</summary>
public sealed record AuditLogStatsDto(
    int EventsToday,
    int UniqueUsers,
    int PhiAccessEvents,
    int FailedAttempts
);
