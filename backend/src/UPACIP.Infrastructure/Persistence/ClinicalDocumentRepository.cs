using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IClinicalDocumentRepository"/> (US_025, AC-001).
/// </summary>
internal sealed class ClinicalDocumentRepository : IClinicalDocumentRepository
{
    private readonly AppDbContext _context;

    public ClinicalDocumentRepository(AppDbContext context) => _context = context;

    /// <inheritdoc />
    public Task<bool> FileHashExistsAsync(Guid patientId, string fileHash, CancellationToken ct = default)
        => _context.ClinicalDocuments
            .AsNoTracking()
            .AnyAsync(d => d.PatientId == patientId && d.FileHash == fileHash, ct);

    /// <inheritdoc />
    public async Task AddAsync(ClinicalDocument document, CancellationToken ct = default)
        => await _context.ClinicalDocuments.AddAsync(document, ct);
}
