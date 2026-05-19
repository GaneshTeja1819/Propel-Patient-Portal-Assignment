using UPACIP.Application.DTOs;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Admin;

/// <summary>
/// Returns today's (UTC) audit summary statistics (AC-001).
/// </summary>
public sealed class GetAuditLogStatsHandler
{
    private readonly IAuditLogReadRepository _repository;

    public GetAuditLogStatsHandler(IAuditLogReadRepository repository)
    {
        _repository = repository;
    }

    public Task<AuditLogStatsDto> HandleAsync(CancellationToken ct = default)
        => _repository.GetStatsAsync(ct);
}
