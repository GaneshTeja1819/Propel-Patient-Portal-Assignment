using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Data access abstraction for <see cref="ClinicalDocument"/> entities.
/// Application layer uses this interface; EF Core implementation lives in Infrastructure.
/// </summary>
public interface IClinicalDocumentRepository
{
    /// <summary>
    /// Returns <c>true</c> when a document with the same SHA-256 hash already
    /// exists for the given patient (duplicate detection — edge case AC-001).
    /// </summary>
    Task<bool> FileHashExistsAsync(Guid patientId, string fileHash, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="ClinicalDocument"/> for insertion on the next SaveChanges.</summary>
    Task AddAsync(ClinicalDocument document, CancellationToken ct = default);
}
