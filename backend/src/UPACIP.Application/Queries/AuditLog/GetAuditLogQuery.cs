namespace UPACIP.Application.Queries.Admin;

/// <summary>
/// Parameters for the filtered, paginated audit log list endpoint (AC-001, AC-002).
/// All filter fields are optional; omitted fields are treated as "no filter".
/// </summary>
public sealed record GetAuditLogQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? ActionCategory,
    string? Role,
    string? Status,
    string? SearchText,
    int Page = 1,
    int PageSize = 15
)
{
    /// <summary>Hard ceiling on page size to prevent runaway memory allocation.</summary>
    public const int MaxPageSize = 100;
}
