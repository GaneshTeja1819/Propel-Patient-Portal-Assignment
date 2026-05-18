using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Persistence operations required by login workflow.
/// </summary>
public interface ILoginUserStore
{
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
