using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Documents;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Documents;

/// <summary>
/// Handles the document upload flow (US_025, AC-001, AC-004, AC-005):
/// <list type="number">
///   <item>Computes SHA-256 hash for per-patient duplicate detection (edge case).</item>
///   <item>Throws <see cref="DuplicateDocumentException"/> (→ HTTP 409) when the same file was already uploaded by this patient.</item>
///   <item>Uploads raw PDF bytes to cloud storage; no binary content enters PostgreSQL (DR-005).</item>
///   <item>Encrypts the returned storage path with <see cref="IEncryptionService"/> (AC-005).</item>
///   <item>Creates a <see cref="ClinicalDocument"/> with <c>ExtractionStatus = "Pending"</c> (AC-001).</item>
///   <item>Writes a <c>DOCUMENT_UPLOADED</c> audit entry (non-blocking — failure does not roll back the upload).</item>
///   <item>Dispatches the AI extraction job via <see cref="IDocumentExtractionJobDispatcher"/> (AC-004).</item>
/// </list>
/// The storage layer throws <c>StorageLimitExceededException</c> on HTTP 507/413; this propagates
/// to the controller which maps it to HTTP 507 (US_025 edge case).
/// </summary>
public sealed class UploadDocumentHandler
{
    private readonly IClinicalDocumentRepository     _repo;
    private readonly IUnitOfWork                     _uow;
    private readonly IDocumentStorageService         _storage;
    private readonly IEncryptionService              _encryption;
    private readonly IAuditLogService                _auditService;
    private readonly IDocumentExtractionJobDispatcher _jobDispatcher;
    private readonly ILogger<UploadDocumentHandler>  _logger;

    public UploadDocumentHandler(
        IClinicalDocumentRepository repo,
        IUnitOfWork uow,
        IDocumentStorageService storage,
        IEncryptionService encryption,
        IAuditLogService auditService,
        IDocumentExtractionJobDispatcher jobDispatcher,
        ILogger<UploadDocumentHandler> logger)
    {
        _repo          = repo;
        _uow           = uow;
        _storage       = storage;
        _encryption    = encryption;
        _auditService  = auditService;
        _jobDispatcher = jobDispatcher;
        _logger        = logger;
    }

    /// <summary>
    /// Executes the upload orchestration.
    /// Throws <see cref="DuplicateDocumentException"/> when a matching file hash is found.
    /// Lets <c>StorageLimitExceededException</c> propagate from the storage layer on quota exhaustion.
    /// </summary>
    public async Task<UploadDocumentResult> HandleAsync(
        UploadDocumentCommand command,
        CancellationToken ct = default)
    {
        // ── 1. SHA-256 duplicate detection ──────────────────────────────────
        var fileHash = ComputeSha256Hex(command.FileBytes);

        var isDuplicate = await _repo.FileHashExistsAsync(command.PatientId, fileHash, ct);
        if (isDuplicate)
            throw new DuplicateDocumentException();

        // ── 2. Upload to cloud storage ───────────────────────────────────────
        // Unique prefix prevents name collisions when the same filename is
        // re-uploaded after a legitimate deletion; SanitizeFileName blocks
        // path-traversal characters from the client-supplied name (OWASP A03).
        var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(command.FileName)}";
        var storagePath = await _storage.UploadAsync(
            command.PatientId,
            uniqueFileName,
            command.FileBytes,
            ct);

        // ── 3. Persist ClinicalDocument ──────────────────────────────────────
        // StoragePath encryption is handled transparently by EF Core's phiConverter
        // (AC-005, DR-005) — do not manually encrypt here to avoid double-encryption.
        var document = new ClinicalDocument
        {
            PatientId        = command.PatientId,
            DocumentType     = command.DocumentType,
            FileName         = command.FileName,
            MimeType         = command.MimeType,
            StoragePath      = storagePath,
            FileHash         = fileHash,
            ExtractionStatus = "Pending",
            UploadedAt       = DateTimeOffset.UtcNow,
        };

        await _repo.AddAsync(document, ct);
        await _uow.SaveChangesAsync(ct);

        // ── 5. Audit log (non-blocking) ──────────────────────────────────────
        try
        {
            await _auditService.LogAsync(
                actorId:           command.PatientId,
                actorRole:         "Patient",
                actionType:        "DOCUMENT_UPLOADED",
                targetEntity:      "ClinicalDocument",
                targetId:          document.Id,
                metadata:          $"{{\"documentType\":\"{command.DocumentType}\",\"fileName\":\"{command.FileName}\"}}",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Audit log write failed for DOCUMENT_UPLOADED on ClinicalDocument {DocumentId}. " +
                "The document was persisted successfully.",
                document.Id);
        }

        // ── 6. Enqueue AI extraction job (AC-004 — within 5 s of upload) ─────
        _jobDispatcher.Dispatch(document.Id);

        return new UploadDocumentResult(document.Id, document.ExtractionStatus);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Removes path separators and shell-special characters from a client-supplied
    /// file name to prevent path traversal (OWASP A03).
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (ch is '/' or '\\' or '\0' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
                continue;
            sb.Append(ch);
        }
        return sb.Length > 0 ? sb.ToString() : "document.pdf";
    }
}

/// <summary>
/// Raised when a patient uploads a file whose SHA-256 hash already exists for their
/// account. Maps to HTTP 409 Conflict (US_025 edge case).
/// </summary>
public sealed class DuplicateDocumentException : Exception
{
    public DuplicateDocumentException()
        : base("This document appears to have been uploaded already.") { }
}
