using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using UPACIP.Application.Commands.Codes;
using UPACIP.Application.Handlers.Codes;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.Handlers.Codes;

/// <summary>
/// Handles the staff code verification flow (US_030, AC-001–AC-005):
/// <list type="number">
///   <item>Guards: duplicate verification (HTTP 409) and finalised encounter (HTTP 422).</item>
///   <item>For Modified decisions: validates <c>verifiedCode</c> against the ICD-10/CPT codeset (AC-003).</item>
///   <item>Creates an immutable <see cref="VerifiedMedicalCode"/> record (INSERT only — AIR-005).</item>
///   <item>Updates <c>MedicalCodeSuggestion.Status</c> to the decision value.</item>
///   <item>Checks all-rejected condition; updates <c>ExtractedClinicalData.CodingStatus</c> (AC-005).</item>
///   <item>Writes an immutable <c>CODE_VERIFIED</c> audit entry (AC-002, AC-004, AIR-005).</item>
/// </list>
/// </summary>
public sealed class VerifyCodeHandler
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _auditService;
    private readonly IIcdCptReferenceService _referenceService;
    private readonly ILogger<VerifyCodeHandler> _logger;

    public VerifyCodeHandler(
        AppDbContext db,
        IUnitOfWork uow,
        IAuditLogService auditService,
        IIcdCptReferenceService referenceService,
        ILogger<VerifyCodeHandler> logger)
    {
        _db               = db;
        _uow              = uow;
        _auditService     = auditService;
        _referenceService = referenceService;
        _logger           = logger;
    }

    /// <summary>
    /// Executes the verify-code orchestration.
    /// Throws <see cref="InvalidOperationException"/> for HTTP 404 (suggestion not found).
    /// Throws <see cref="ConflictException"/> for HTTP 409 (already verified).
    /// Throws <see cref="UnprocessableEntityException"/> for HTTP 422 (invalid code or finalised).
    /// </summary>
    public async Task<VerifyCodeResult> HandleAsync(
        VerifyCodeCommand command,
        CancellationToken ct = default)
    {
        // ── 1. Load suggestion + clinical data ──────────────────────────
        var suggestion = await _db.MedicalCodeSuggestions
            .Include(m => m.ClinicalData)
            .FirstOrDefaultAsync(m => m.Id == command.SuggestionId, ct);

        if (suggestion is null)
            throw new InvalidOperationException(
                $"MedicalCodeSuggestion {command.SuggestionId} not found.");

        // ── 1b. Defensive Decision guard ─────────────────────────────────
        // Validates the Decision value even when the handler is called programmatically
        // (outside the HTTP boundary where [RegularExpression] applies — GAP-004 fix).
        if (command.Decision is not ("Accepted" or "Modified" or "Rejected"))
            throw new UnprocessableEntityException(
                $"Invalid decision '{command.Decision}'. Must be Accepted, Modified, or Rejected.");

        // ── 2. Duplicate-verification guard (edge case) ──────────────────
        // Check before finalized-encounter guard so a repeat call returns 409,
        // not 422, even after the encounter has been marked Complete.
        var alreadyVerified = await _db.VerifiedMedicalCodes
            .AnyAsync(v => v.SuggestionId == command.SuggestionId, ct);

        if (alreadyVerified)
            throw new ConflictException("Code already verified.");

        // ── 3. Finalised-encounter guard (AC-005 edge case) ──────────────
        // Inferred decision: no Encounter entity exists in domain; "Finalized"
        // maps to ExtractedClinicalData.CodingStatus == "Complete" (US_030, task_003).
        if (suggestion.ClinicalData.CodingStatus == "Complete")
            throw new UnprocessableEntityException(
                "This encounter has been finalised.");

        // ── 4. Codeset validation for Modified decision (AC-003) ─────────
        if (command.Decision == "Modified")
        {
            if (string.IsNullOrWhiteSpace(command.VerifiedCode))
                throw new UnprocessableEntityException(
                    "verifiedCode is required when decision is 'Modified'.");

            if (!_referenceService.IsValidCode(command.VerifiedCode, suggestion.CodeSystem))
                throw new UnprocessableEntityException(
                    "Code not found in codeset.");
        }

        // ── 5. Determine code value to store ─────────────────────────────
        var finalCode = command.Decision switch
        {
            "Modified"  => command.VerifiedCode!,
            "Accepted"  => suggestion.SuggestedCode,
            _           => string.Empty          // Rejected
        };

        var originalCode = command.Decision == "Modified"
            ? suggestion.SuggestedCode
            : null;

        // ── 6. Create immutable VerifiedMedicalCode (INSERT only — AIR-005)
        var verified = new VerifiedMedicalCode
        {
            SuggestionId          = suggestion.Id,
            VerifiedById          = command.ActorStaffId,
            CodeSystem            = suggestion.CodeSystem,
            Code                  = finalCode,
            Description           = suggestion.Description,
            Decision              = command.Decision,
            OriginalSuggestedCode = originalCode,
            VerifiedAt            = DateTimeOffset.UtcNow,
        };

        _db.VerifiedMedicalCodes.Add(verified);

        // ── 7. Mirror decision back to MedicalCodeSuggestion.Status ──────
        suggestion.Status     = command.Decision;
        suggestion.IsVerified = true;

        // ── 8. All-rejected check + CodingStatus update (AC-005) ─────────
        var codingStatusUpdated = false;
        var clinicalData = suggestion.ClinicalData;

        // Reload all suggestions for this clinical data record to evaluate
        // the all-rejected condition correctly.
        var allSuggestions = await _db.MedicalCodeSuggestions
            .Where(m => m.ClinicalDataId == suggestion.ClinicalDataId)
            .ToListAsync(ct);

        var allWillBeRejected = command.Decision == "Rejected"
            && allSuggestions.All(m => m.Id == suggestion.Id || m.Status == "Rejected");

        if (allWillBeRejected)
        {
            clinicalData.CodingStatus = "PendingManualCoding";
            codingStatusUpdated = true;
        }
        else if (command.Decision is "Accepted" or "Modified")
        {
            clinicalData.CodingStatus = "Complete";
            codingStatusUpdated = true;
        }

        // ── 8b. Persist — wrapped to surface unique-constraint as HTTP 409 (GAP-001 fix) ──
        // Under concurrency, two requests can both pass the AnyAsync duplicate check before
        // either commits. The unique index on verified_medical_codes.suggestion_id (SQLSTATE
        // 23505) would otherwise propagate as an unhandled DbUpdateException (HTTP 500).
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException dbEx)
            when (FindSqlState(dbEx) == "23505")
        {
            throw new ConflictException("Code already verified.");
        }

        // ── 9. Audit (non-blocking — failure must not roll back persist) ──
        // Use JsonSerializer to prevent JSON-injection from DB-originated AI values (GAP-003 fix).
        var metadata = JsonSerializer.Serialize(new
        {
            suggestionId          = suggestion.Id,
            decision              = command.Decision,
            verifiedCode          = finalCode,
            originalSuggestedCode = originalCode ?? string.Empty,
            codeSystem            = suggestion.CodeSystem,
        });

        try
        {
            await _auditService.LogAsync(
                actorId:           command.ActorStaffId,
                actorRole:         "Staff",
                actionType:        "CODE_VERIFIED",
                targetEntity:      "VerifiedMedicalCode",
                targetId:          verified.Id,
                metadata:          metadata,
                cancellationToken: ct);

            if (allWillBeRejected)
            {
                await _auditService.LogAsync(
                    actorId:           command.ActorStaffId,
                    actorRole:         "Staff",
                    actionType:        "MASS_REJECTION",
                    targetEntity:      "ExtractedClinicalData",
                    targetId:          suggestion.ClinicalDataId,
                    metadata:          $"{{\"clinicalDataId\":\"{suggestion.ClinicalDataId}\"}}",
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Audit log write failed for CODE_VERIFIED on suggestion {SuggestionId}. " +
                "VerifiedMedicalCode {VerifiedId} was persisted successfully.",
                suggestion.Id,
                verified.Id);
        }

        return new VerifyCodeResult(verified.Id, codingStatusUpdated);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static string? FindSqlState(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner is not null)
        {
            if (inner is PostgresException pg)
                return pg.SqlState;
            inner = inner.InnerException;
        }
        return null;
    }
}
