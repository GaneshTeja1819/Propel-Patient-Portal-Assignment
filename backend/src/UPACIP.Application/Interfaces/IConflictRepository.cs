using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Data-access contract for <see cref="DataConflict"/> records.
/// Implemented in Infrastructure.Persistence.
/// </summary>
public interface IConflictRepository
{
    /// <summary>Returns a tracked DataConflict by ID, or null if not found.</summary>
    Task<DataConflict?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns all DataConflict records for a patient, newest first.
    /// Includes a boolean computed by comparing <c>CreatedAt</c> to
    /// <paramref name="lastReviewedAt"/>.
    /// </summary>
    Task<IReadOnlyList<DataConflict>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>
    /// Returns the patient's LastConflictReviewedAt timestamp for IsNew computation.
    /// Returns null when the patient record is not found.
    /// </summary>
    Task<DateTimeOffset?> GetLastReviewedAtAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>Sets User.LastConflictReviewedAt = UtcNow for the given patient.</summary>
    Task TouchLastReviewedAtAsync(Guid patientId, CancellationToken ct = default);
}
