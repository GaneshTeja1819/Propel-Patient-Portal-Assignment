using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;
using UPACIP.Infrastructure.AI;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire background job that generates AI ICD-10 and CPT code suggestions for a
/// patient's extracted clinical data (US_029, AC-001 through AC-005).
///
/// Pipeline stages:
/// <list type="number">
///   <item>Idempotency guard: exit if any <see cref="MedicalCodeSuggestion"/> with
///         <c>Status ∈ [Pending, Processing]</c> already exists for the same
///         <c>ExtractedClinicalDataId</c> (AC-001 edge case — concurrent jobs).</item>
///   <item>Insert a sentinel "Processing" row so concurrent runs are rejected.</item>
///   <item>Load <see cref="ExtractedClinicalData"/>; decrypt clinical JSON.</item>
///   <item>Invoke Gemini via <see cref="GeminiCodingAdapter"/>.</item>
///   <item>Validate and persist <see cref="MedicalCodeSuggestion"/> records (AC-002).</item>
///   <item>Zero-suggestions path: write <c>CODE_SUGGESTION_EMPTY</c> audit (AC-003).</item>
/// </list>
///
/// <para>
/// <c>AI_INVOCATION</c> audit entries (AC-004, AIR-006) are written automatically by
/// <see cref="GeminiInvocationLogger"/> for every Gemini call — no manual audit writes
/// are required for the Gemini call itself.
/// </para>
///
/// <para>
/// Retry behaviour: <see cref="AutomaticRetryAttribute"/> applies one retry at 60 s back-off
/// (AC-005 "retry exactly once"). On final failure the sentinel row is removed, a
/// <c>CODE_SUGGESTION_FAILED</c> audit entry is written, and <see cref="ExtractedClinicalData"/>
/// is left unchanged (AC-005).
/// </para>
/// </summary>
[AutomaticRetry(Attempts = 1, DelaysInSeconds = new[] { 60 })]
public sealed class CodeSuggestionJob
{
    private const int MaxRetryAttempts = 1;

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly GeminiCodingAdapter _gemini;
    private readonly IAuditLogService _auditLog;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CodeSuggestionJob> _logger;

    public CodeSuggestionJob(
        AppDbContext db,
        GeminiCodingAdapter gemini,
        IAuditLogService auditLog,
        IUnitOfWork uow,
        ILogger<CodeSuggestionJob> logger)
    {
        _db       = db;
        _gemini   = gemini;
        _auditLog = auditLog;
        _uow      = uow;
        _logger   = logger;
    }

    /// <summary>
    /// Entry point invoked by Hangfire. See class summary for pipeline description.
    /// <para>
    /// <paramref name="context"/> is injected automatically by Hangfire; it is never
    /// serialised as part of the job arguments.
    /// </para>
    /// </summary>
    public async Task ExecuteAsync(Guid extractedClinicalDataId, PerformContext? context = null)
    {
        _logger.LogInformation(
            "Code suggestion job started. ExtractedClinicalDataId={Id}.", extractedClinicalDataId);

        // ── 1. Idempotency guard (AC-001 edge case) ───────────────────────
        var activeSuggestionExists = await _db.MedicalCodeSuggestions
            .AnyAsync(m => m.ClinicalDataId == extractedClinicalDataId
                        && (m.Status == "Pending" || m.Status == "Processing"));

        if (activeSuggestionExists)
        {
            _logger.LogWarning(
                "Active suggestion set already exists for ExtractedClinicalDataId={Id}. Exiting.",
                extractedClinicalDataId);
            return;
        }

        // ── 2. Load ExtractedClinicalData ─────────────────────────────────
        var clinicalData = await _db.ExtractedClinicalData
            .FirstOrDefaultAsync(e => e.Id == extractedClinicalDataId);

        if (clinicalData is null)
        {
            _logger.LogError(
                "ExtractedClinicalData {Id} not found — job cannot proceed.", extractedClinicalDataId);
            return;
        }

        // ── 3. Insert a sentinel "Processing" row to block concurrent runs ─
        // A single sentinel allows the idempotency guard above to detect in-flight jobs.
        var sentinel = new MedicalCodeSuggestion
        {
            ClinicalDataId = extractedClinicalDataId,
            PatientId      = clinicalData.PatientId,
            CodeSystem     = "SENTINEL",
            SuggestedCode  = string.Empty,
            Description    = string.Empty,
            Rank           = 0,
            Status         = "Processing",
            ModelVersion   = string.Empty,
            PromptHash     = string.Empty,
        };
        await _db.MedicalCodeSuggestions.AddAsync(sentinel);
        await _uow.SaveChangesAsync();

        try
        {
            // ── 4. Invoke Gemini for code suggestion ──────────────────────
            // GeminiInvocationLogger decorator auto-writes AI_INVOCATION audit (AC-004, AIR-006).
            // EncryptedExtractedJson is already decrypted by EF Core on load (AC-002).
            var codingResult = await _gemini.SuggestCodesAsync(
                clinicalData.EncryptedExtractedJson);

            // ── 5. Remove sentinel row ────────────────────────────────────
            _db.MedicalCodeSuggestions.Remove(sentinel);

            // ── 6. Zero-suggestions path (AC-003) ─────────────────────────
            if (codingResult.Suggestions.Length == 0)
            {
                await _uow.SaveChangesAsync();

                await _auditLog.LogAsync(
                    actorId:      clinicalData.PatientId,
                    actorRole:    "System",
                    actionType:   "CODE_SUGGESTION_EMPTY",
                    targetEntity: "ExtractedClinicalData",
                    targetId:     extractedClinicalDataId,
                    metadata:     "Gemini returned zero code candidates.");

                _logger.LogInformation(
                    "No code candidates returned for ExtractedClinicalDataId={Id}.", extractedClinicalDataId);
                return;
            }

            // ── 7. Persist MedicalCodeSuggestion records (AC-002) ────────
            var modelVersion = "gemini-1.5-pro";
            var promptHash   = _gemini.PromptHash;
            var now          = DateTimeOffset.UtcNow;

            foreach (var item in codingResult.Suggestions)
            {
                var suggestion = new MedicalCodeSuggestion
                {
                    ClinicalDataId = extractedClinicalDataId,
                    PatientId      = clinicalData.PatientId,
                    CodeSystem     = item.CodeType,        // "ICD10" | "CPT"
                    SuggestedCode  = item.Code,
                    Description    = item.Description,
                    ConfidenceScore = item.ConfidenceScore,
                    Rank           = item.Rank,
                    Status         = "Pending",            // awaiting Staff verification (UC-024)
                    ModelVersion   = modelVersion,
                    PromptHash     = promptHash,
                    SuggestedAt    = now,
                };
                await _db.MedicalCodeSuggestions.AddAsync(suggestion);
            }

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Code suggestion completed. ExtractedClinicalDataId={Id}, Count={Count}.",
                extractedClinicalDataId, codingResult.Suggestions.Length);
        }
        catch (SchemaVersionMismatchException ex)
        {
            // Non-retriable: schema mismatch must not loop through back-off retries.
            _logger.LogError(ex,
                "Schema version mismatch during code suggestion for ExtractedClinicalDataId={Id}.",
                extractedClinicalDataId);
            await RemoveSentinelAndWriteFailedAuditAsync(sentinel, clinicalData, ex.Message);
        }
        catch (Exception ex)
        {
            var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;

            _logger.LogError(ex,
                "Code suggestion attempt {Attempt}/{MaxAttempts} failed. ExtractedClinicalDataId={Id}.",
                retryCount + 1, MaxRetryAttempts + 1, extractedClinicalDataId);

            if (retryCount >= MaxRetryAttempts)
            {
                // All retries exhausted (AC-005): ExtractedClinicalData unchanged.
                await RemoveSentinelAndWriteFailedAuditAsync(sentinel, clinicalData, $"{ex.GetType().Name}: {ex.Message}");
                return;
            }

            // Not the final attempt — keep sentinel, re-throw for AutomaticRetry back-off.
            throw;
        }
    }

    private async Task RemoveSentinelAndWriteFailedAuditAsync(
        MedicalCodeSuggestion sentinel,
        ExtractedClinicalData clinicalData,
        string reason)
    {
        _db.MedicalCodeSuggestions.Remove(sentinel);
        await _uow.SaveChangesAsync();

        await _auditLog.LogAsync(
            actorId:      clinicalData.PatientId,
            actorRole:    "System",
            actionType:   "CODE_SUGGESTION_FAILED",
            targetEntity: "ExtractedClinicalData",
            targetId:     clinicalData.Id,
            metadata:     reason);

        _logger.LogWarning(
            "Code suggestion failed. ExtractedClinicalDataId={Id}, Reason={Reason}.",
            clinicalData.Id, reason);
    }
}
