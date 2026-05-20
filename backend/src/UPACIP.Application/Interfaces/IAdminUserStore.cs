using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Persistence operations required by admin user-management handlers.
/// </summary>
public interface IAdminUserStore
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<User>> SearchUsersAsync(string? q, CancellationToken ct = default);

    /// <summary>
    /// Returns <see langword="true"/> when the <paramref name="normalizedEmail"/> is already
    /// taken by a user other than <paramref name="excludeUserId"/>.
    /// Pass <see langword="null"/> for <paramref name="excludeUserId"/> to check all users
    /// (e.g., during create).
    /// </summary>
    Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludeUserId = null, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}
