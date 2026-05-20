using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IConflictRepository"/>.
/// </summary>
public sealed class ConflictRepository : IConflictRepository
{
    private readonly AppDbContext _db;

    public ConflictRepository(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<DataConflict?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.DataConflicts
              .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DataConflict>> GetByPatientIdAsync(
        Guid patientId,
        CancellationToken ct = default)
        => await _db.DataConflicts
                    .Where(c => c.PatientId == patientId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetLastReviewedAtAsync(
        Guid patientId,
        CancellationToken ct = default)
        => await _db.Users
                    .Where(u => u.Id == patientId)
                    .Select(u => u.LastConflictReviewedAt)
                    .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task TouchLastReviewedAtAsync(Guid patientId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == patientId, ct);
        if (user is not null)
            user.LastConflictReviewedAt = DateTimeOffset.UtcNow;
        // Caller (handler) is responsible for calling IUnitOfWork.SaveChangesAsync.
    }
}
