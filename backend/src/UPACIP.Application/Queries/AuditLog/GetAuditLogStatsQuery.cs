namespace UPACIP.Application.Queries.Admin;

/// <summary>
/// Marker record for the today's audit stats query (AC-001).
/// No parameters — scope is always UTC today, computed server-side.
/// </summary>
public sealed record GetAuditLogStatsQuery();
