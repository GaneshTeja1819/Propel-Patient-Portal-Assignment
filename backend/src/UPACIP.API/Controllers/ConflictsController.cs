using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using UPACIP.Application.Commands.Conflicts;
using UPACIP.Application.Handlers.Conflicts;
using UPACIP.Application.Interfaces;

namespace UPACIP.API.Controllers;

/// <summary>
/// Conflict management endpoints (US_028).
/// All endpoints require StaffPolicy — Patient requests return HTTP 403 (AC-005).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffPolicy")]
[Route("api/v{version:apiVersion}")]
public sealed class ConflictsController : ControllerBase
{
    private readonly IConflictRepository _repo;
    private readonly ResolveConflictHandler _resolveHandler;
    private readonly MarkReviewedHandler _markReviewedHandler;
    private readonly ILogger<ConflictsController> _logger;

    public ConflictsController(
        IConflictRepository repo,
        ResolveConflictHandler resolveHandler,
        MarkReviewedHandler markReviewedHandler,
        ILogger<ConflictsController> logger)
    {
        _repo               = repo;
        _resolveHandler     = resolveHandler;
        _markReviewedHandler = markReviewedHandler;
        _logger             = logger;
    }

    /// <summary>
    /// Returns all DataConflict records for a patient (AC-001, AC-004).
    /// Returns HTTP 200 with an empty array when no conflicts exist.
    /// </summary>
    [HttpGet("patients/{patientId:guid}/conflicts")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IReadOnlyList<DataConflictDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConflicts(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var conflicts = await _repo.GetByPatientIdAsync(patientId, cancellationToken);
        var lastReviewedAt = await _repo.GetLastReviewedAtAsync(patientId, cancellationToken);

        var dtos = conflicts
            .Select(c => MapToDto(c, lastReviewedAt))
            .ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Resolves a conflict with an authoritative value (AC-002).
    /// Returns HTTP 409 when the conflict is already resolved or when a concurrent
    /// update is detected (optimistic concurrency / xmin guard).
    /// </summary>
    [HttpPatch("conflicts/{id:guid}/resolve")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResolveConflict(
        Guid id,
        [FromBody] ResolveConflictRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = ResolveActorId();
        var command = new ResolveConflictCommand(
            ConflictId:        id,
            ActorId:           actorId,
            AuthoritativeValue: request.AuthoritativeValue,
            SourceDocumentId:  request.SourceDocumentId,
            ResolutionNote:    request.ResolutionNote);

        try
        {
            await _resolveHandler.HandleAsync(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Conflict not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Conflict already resolved by another request." });
        }
    }

    /// <summary>
    /// Marks a conflict as reviewed without resolving it (AC-003).
    /// Sets status to "ReviewedUnresolved" and writes audit.
    /// </summary>
    [HttpPatch("conflicts/{id:guid}/mark-reviewed")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkReviewed(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = ResolveActorId();
        var command = new MarkReviewedCommand(ConflictId: id, ActorId: actorId);

        try
        {
            await _markReviewedHandler.HandleAsync(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Conflict not found." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Guid ResolveActorId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private DataConflictDto MapToDto(
        UPACIP.Domain.Entities.DataConflict c,
        DateTimeOffset? lastReviewedAt)
    {
        var values = DeserializeConflictingValues(c.ConflictingValues, c.Id, _logger);
        var isNew = lastReviewedAt is null || c.CreatedAt > lastReviewedAt.Value;

        return new DataConflictDto(
            Id:                c.Id,
            PatientId:         c.PatientId,
            FieldName:         c.FieldName,
            Severity:          c.Severity,
            Status:            c.Status,
            ConflictingValues: values,
            CanonicalValue:    c.CanonicalValue,
            IsNew:             isNew,
            DetectedAt:        c.CreatedAt,
            ResolvedAt:        c.ResolvedAt,
            ResolvedById:      c.ResolvedById);
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static IReadOnlyList<ConflictingValueDto> DeserializeConflictingValues(string json, Guid conflictId, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<ConflictingValueDto>>(json, JsonOpts) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "ConflictingValues JSON for conflict {ConflictId} could not be deserialized. Returning empty list.",
                conflictId);
            return [];
        }
    }
}

// ── DTOs ───────────────────────────────────────────────────────────────────────

/// <summary>Request body for PATCH /resolve.</summary>
public sealed record ResolveConflictRequest(
    [Required, MinLength(1)] string AuthoritativeValue,
    string? SourceDocumentId,
    string? ResolutionNote);

/// <summary>Single competing source value within a conflict.</summary>
public sealed record ConflictingValueDto(
    string Value,
    string? SourceLabel,
    string? SourceDocumentId,
    bool IsAiExtracted);

/// <summary>Full conflict DTO returned by GET /patients/{id}/conflicts.</summary>
public sealed record DataConflictDto(
    Guid Id,
    Guid PatientId,
    string FieldName,
    string Severity,
    string Status,
    IReadOnlyList<ConflictingValueDto> ConflictingValues,
    string? CanonicalValue,
    bool IsNew,
    DateTimeOffset DetectedAt,
    DateTimeOffset? ResolvedAt,
    Guid? ResolvedById);
