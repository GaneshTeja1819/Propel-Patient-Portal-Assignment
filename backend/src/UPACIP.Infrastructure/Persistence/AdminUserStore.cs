using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAdminUserStore"/>.
/// Provides user-lookup and search queries needed by admin management handlers.
/// </summary>
internal sealed class AdminUserStore : IAdminUserStore
{
    private readonly AppDbContext _context;

    public AdminUserStore(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> SearchUsersAsync(string? q, CancellationToken ct = default)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            // PostgreSQL ILIKE via EF Core: use EF.Functions.ILike for case-insensitive match.
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{term}%") ||
                EF.Functions.ILike(u.Email, $"%{term}%"));
        }

        return await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludeUserId = null,
        CancellationToken ct = default)
    {
        var query = _context.Users.Where(u => u.Email == normalizedEmail);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return query.AnyAsync(ct);
    }

    public Task AddAsync(User user, CancellationToken ct = default)
        => _context.Users.AddAsync(user, ct).AsTask();
}
