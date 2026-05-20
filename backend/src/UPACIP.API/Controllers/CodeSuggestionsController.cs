using Asp.Versioning;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using UPACIP.Infrastructure.BackgroundJobs;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.API.Controllers;

/// <summary>
/// On-demand code suggestion trigger endpoint (US_029, AC-001 on-demand path).
///
/// All endpoints require the <c>StaffPolicy</c> role (OWASP A01, AC-005).
/// Auto-triggered code suggestion is handled by <see cref="CodeSuggestionJob"/>
/// enqueued as a Hangfire continuation from <see cref="ClinicalDataExtractionJob"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffPolicy")]
[Route("api/v{version:apiVersion}/code-suggestions")]
public sealed class CodeSuggestionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobClient;

    public CodeSuggestionsController(AppDbContext db, IBackgroundJobClient jobClient)
    {
        _db        = db;
        _jobClient = jobClient;
    }

    // ── GET /api/v1/code-suggestions?encounterId={id} ───────────────────

    /// <summary>
    /// Returns all <see cref="MedicalCodeSuggestion"/> records for the specified
    /// extracted clinical data record (encounter), ordered by <c>Rank</c> (AC-001).
    /// Includes verification status derived from the linked <c>VerifiedMedicalCode</c>.
    /// </summary>
    /// <param name="encounterId">
    /// The <c>ExtractedClinicalData.Id</c> representing the encounter (US_030, task_002).
    /// </param>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalCodeSuggestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEncounter(
        [FromQuery][Required] Guid encounterId,
        CancellationToken cancellationToken)
    {
        // Ownership / existence check (OWASP A01 — no silent 200 with empty list on unknown ID).
        var encounterExists = await _db.ExtractedClinicalData
            .AnyAsync(e => e.Id == encounterId, cancellationToken);

        if (!encounterExists)
            return NotFound(new { message = "ExtractedClinicalData record not found." });

        var suggestions = await _db.MedicalCodeSuggestions
            .Include(m => m.VerifiedCode)
            .Where(m => m.ClinicalDataId == encounterId)
            .OrderBy(m => m.Rank)
            .Select(m => new MedicalCodeSuggestionDto(
                m.Id,
                m.ClinicalDataId,
                m.CodeSystem,
                m.SuggestedCode,
                m.Description,
                m.ConfidenceScore,
                m.Rank,
                m.Status,
                m.SuggestedAt,
                m.VerifiedCode == null ? null : new VerifiedCodeSummaryDto(
                    m.VerifiedCode.Id,
                    m.VerifiedCode.Decision,
                    m.VerifiedCode.Code,
                    m.VerifiedCode.OriginalSuggestedCode,
                    m.VerifiedCode.VerifiedAt)))
            .ToListAsync(cancellationToken);

        return Ok(suggestions);
    }

    // ── POST /api/v1/code-suggestions/generate ───────────────────────────

    /// <summary>
    /// Queues a <see cref="CodeSuggestionJob"/> for the specified extracted clinical data record.
    /// Returns HTTP 202 with the Hangfire job ID, or HTTP 409 if a suggestion run is already in
    /// progress for the same record.
    /// </summary>
    /// <remarks>
    /// Used by the "Regenerate" CTA on SCR-014 (AC-001 on-demand path, edge case).
    /// </remarks>
    [HttpPost("generate")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GenerateCodeSuggestionsResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateCodeSuggestionsRequest request,
        CancellationToken cancellationToken)
    {
        // Validate the ExtractedClinicalData record exists (OWASP A01 — no silent leaks).
        var exists = await _db.ExtractedClinicalData
            .AnyAsync(e => e.Id == request.ExtractedClinicalDataId, cancellationToken);

        if (!exists)
            return NotFound(new { message = "ExtractedClinicalData record not found." });

        // Idempotency check: reject if a Pending/Processing set already exists (AC-001 edge case).
        var activeExists = await _db.MedicalCodeSuggestions
            .AnyAsync(m => m.ClinicalDataId == request.ExtractedClinicalDataId
                        && (m.Status == "Pending" || m.Status == "Processing"),
                      cancellationToken);

        if (activeExists)
            return Conflict(new { message = "Suggestion generation already in progress." });

        var jobId = _jobClient.Enqueue<CodeSuggestionJob>(
            j => j.ExecuteAsync(request.ExtractedClinicalDataId, null));

        return Accepted(new GenerateCodeSuggestionsResponse(JobId: jobId));
    }
}

// ── Request / Response types ───────────────────────────────────────────────

/// <summary>Request body for POST /generate.</summary>
public sealed record GenerateCodeSuggestionsRequest(
    [Required] Guid ExtractedClinicalDataId);

/// <summary>Response body for POST /generate.</summary>
public sealed record GenerateCodeSuggestionsResponse(string JobId);

/// <summary>Response DTO for a single suggestion in GET /code-suggestions.</summary>
public sealed record MedicalCodeSuggestionDto(
    Guid Id,
    Guid ClinicalDataId,
    string CodeSystem,
    string SuggestedCode,
    string Description,
    double ConfidenceScore,
    int Rank,
    string Status,
    DateTimeOffset SuggestedAt,
    VerifiedCodeSummaryDto? VerifiedCode);

/// <summary>
/// Inline summary of the linked <c>VerifiedMedicalCode</c> record
/// when the suggestion has been reviewed by staff.
/// </summary>
public sealed record VerifiedCodeSummaryDto(
    Guid Id,
    string Decision,
    string Code,
    string? OriginalSuggestedCode,
    DateTimeOffset VerifiedAt);
