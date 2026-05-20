using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UPACIP.Application.Commands.Documents;
using UPACIP.Application.Handlers.Documents;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.Documents;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.API.Controllers;

/// <summary>
/// Clinical document management endpoints (US_025, AC-001).
///
/// All endpoints require the <c>PatientPolicy</c> role — only authenticated
/// patients may upload their own documents (OWASP A01).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "PatientPolicy")]
[Route("api/v{version:apiVersion}/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentHandler _uploadHandler;
    private readonly AppDbContext _db;
    private readonly IDocumentExtractionJobDispatcher _dispatcher;

    public DocumentsController(
        UploadDocumentHandler uploadHandler,
        AppDbContext db,
        IDocumentExtractionJobDispatcher dispatcher)
    {
        _uploadHandler = uploadHandler;
        _db            = db;
        _dispatcher    = dispatcher;
    }

    // ── POST /api/v1/documents/upload ────────────────────────────────────────

    /// <summary>
    /// Uploads a clinical PDF document for the authenticated patient.
    /// Stores the file in Supabase Storage, creates a <c>ClinicalDocument</c>
    /// record, writes an audit entry, and enqueues an AI extraction job.
    /// Returns HTTP 201 with the new document ID and extraction status on success.
    /// Returns HTTP 409 when the same file has already been uploaded by this patient.
    /// Returns HTTP 507 when the storage quota is exhausted.
    /// </summary>
    /// <param name="file">PDF file (multipart/form-data).</param>
    /// <param name="documentType">Document category (e.g. "Historical", "Lab").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("upload")]
    [MapToApiVersion("1.0")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status507InsufficientStorage)]
    [RequestSizeLimit(11_534_336)]  // 11 MB hard cap (10 MB file + multipart overhead)
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        [FromForm] string documentType,
        CancellationToken ct)
    {
        // ── Extract patient identity from JWT sub claim (OWASP A01) ──────
        var sub = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var patientId))
            return Unauthorized(new { message = "Invalid or missing identity claim." });

        // ── Input validation ─────────────────────────────────────────────
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (string.IsNullOrWhiteSpace(documentType))
            return BadRequest(new { message = "documentType is required." });

        // Read file bytes into a managed buffer (max enforced by RequestSizeLimit).
        byte[] fileBytes;
        using (var ms = new MemoryStream((int)Math.Min(file.Length, 10 * 1024 * 1024)))
        {
            await file.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
        }

        var command = new UploadDocumentCommand(
            PatientId:    patientId,
            DocumentType: documentType,
            FileName:     file.FileName,
            MimeType:     file.ContentType,
            FileBytes:    fileBytes);

        try
        {
            var result = await _uploadHandler.HandleAsync(command, ct);
            return StatusCode(
                StatusCodes.Status201Created,
                new UploadDocumentResponse(result.ClinicalDocumentId, result.ExtractionStatus));
        }
        catch (DuplicateDocumentException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (StorageLimitExceededException ex)
        {
            return StatusCode(
                StatusCodes.Status507InsufficientStorage,
                new { message = ex.Message });
        }
    }

    // ── GET /api/v1/documents/{id}/extraction-status ─────────────────────

    /// <summary>
    /// Returns the current AI extraction status for the specified document (UXR-603).
    /// Only the owning patient may query their own document (OWASP A01 — IDOR guard).
    /// </summary>
    /// <param name="id">Clinical document identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}/extraction-status")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ExtractionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExtractionStatus(Guid id, CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var patientId))
            return Unauthorized(new { message = "Invalid or missing identity claim." });

        // Project only the status fields — avoids loading PHI columns (OWASP A01/A02).
        var status = await _db.ClinicalDocuments
            .AsNoTracking()
            .Where(d => d.Id == id && d.PatientId == patientId)
            .Select(d => new ExtractionStatusResponse(d.ExtractionStatus, d.ExtractionFailureNote))
            .FirstOrDefaultAsync(ct);

        if (status is null)
            return NotFound();

        return Ok(status);
    }

    // ── POST /api/v1/documents/{id}/retry-extraction ──────────────────────

    /// <summary>
    /// Resets a failed extraction and enqueues a fresh extraction job (AC-004, UXR-603).
    /// Returns HTTP 202 Accepted on success.
    /// Returns HTTP 409 Conflict when the document is not in the "Failed" state.
    /// Only the owning patient may retry their own document (OWASP A01 — IDOR guard).
    /// </summary>
    /// <param name="id">Clinical document identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{id:guid}/retry-extraction")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryExtraction(Guid id, CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var patientId))
            return Unauthorized(new { message = "Invalid or missing identity claim." });

        // Ownership check (OWASP A01 — IDOR) is enforced via the patientId filter.
        var document = await _db.ClinicalDocuments
            .Where(d => d.Id == id && d.PatientId == patientId)
            .FirstOrDefaultAsync(ct);

        if (document is null)
            return NotFound();

        if (document.ExtractionStatus != "Failed")
            return Conflict(new { message = "Retry is only allowed when extraction status is 'Failed'." });

        document.ExtractionStatus     = "Pending";
        document.ExtractionFailureNote = null;
        await _db.SaveChangesAsync(ct);

        _dispatcher.Dispatch(id);

        return Accepted();
    }
}

/// <summary>Response DTO for <c>GET /extraction-status</c> (UXR-603).</summary>
/// <param name="ExtractionStatus">Current pipeline state: Pending | Processing | Completed | Failed.</param>
/// <param name="ExtractionFailureNote">Human-readable failure reason; <c>null</c> when not Failed.</param>
internal sealed record ExtractionStatusResponse(
    string ExtractionStatus,
    string? ExtractionFailureNote);

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Response body for <c>POST /documents/upload</c>.</summary>
public sealed record UploadDocumentResponse(
    Guid ClinicalDocumentId,
    string ExtractionStatus);
