namespace UPACIP.Application.DTOs;

/// <summary>Paginated result wrapper returned by all list endpoints.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    int Total,
    int Page,
    int PageSize
);
