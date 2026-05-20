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
/// Hangfire background job that drives the AI de-duplication pipeline for a patient's
/// extracted clinical data (US_027, AC-003, AIR-004).
///
/// Pipeline stages:
/// <list type="number">
///   <item>Idempotency guard: exit if <c>DeduplicationStatus</c> is already "Processing".</item>
///   <item>Transition <c>DeduplicationStatus</c>: Pending/Failed → Processing.</item>
///   <item>Load all <see cref="ExtractedClinicalData"/> for the patient (EF auto-decrypts PHI).</item>
///   <item>Flatten extraction results into a de-duplication input list.</item>
///   <item>Call Gemini via <see cref="GeminiDeduplicationAdapter"/>; validate schema (AIR-004).</item>
///   <item>Replace old <see cref="MergedClinicalEntry"/> records for the patient.</item>
///   <item>Persist new canonical entries (EF auto-encrypts PHI on SaveChanges, AC-003).</item>
///   <item>Transition <c>DeduplicationStatus</c>: Processing → Completed.</item>
///   <item>Write <c>DEDUP_COMPLETED</c> audit entry (AC-003).</item>
/// </list>
///
/// <para>
/// Retry behaviour: <see cref="AutomaticRetryAttribute"/> applies two retries at 30 s and
/// 300 s back-off (AC-005). On all-attempts failure the status is set to "Failed".
/// </para>
/// </summary>
[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 300 })]
internal sealed class DeduplicationJob
{
    private const int MaxRetryAttempts = 2;

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly GeminiDeduplicationAdapter _gemini;
    private readonly IAuditLogService _auditLog;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeduplicationJob> _logger;

    public DeduplicationJob(
        AppDbContext db,
        GeminiDeduplicationAdapter gemini,
        IAuditLogService auditLog,
        IUnitOfWork uow,
        ILogger<DeduplicationJob> logger)
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
    public async Task ExecuteAsync(Guid patientId, PerformContext? context = null)
    {
        _logger.LogInformation(
            "Deduplication job started. PatientId={PatientId}.", patientId);

        // ── 1. Load or create PatientProfile360 ──────────────────────────
        var profile = await _db.PatientProfiles360
            .FirstOrDefaultAsync(p => p.PatientId == patientId);

        if (profile is null)
        {
            profile = new PatientProfile360
            {
                PatientId           = patientId,
                DeduplicationStatus = "Pending",
            };
            await _db.PatientProfiles360.AddAsync(profile);
            await _uow.SaveChangesAsync();
        }

        // ── 2. Idempotency guard (edge case: concurrent Hangfire retry) ───
        if (string.Equals(profile.DeduplicationStatus, "Processing", StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Deduplication already in progress for PatientId={PatientId}. Exiting.", patientId);
            return;
        }

        try
        {
            // ── 3. Transition → Processing ────────────────────────────────
            profile.DeduplicationStatus = "Processing";
            profile.LastUpdatedAt       = DateTimeOffset.UtcNow;
            await _uow.SaveChangesAsync();

            // ── 4. Load extracted clinical data (EF auto-decrypts PHI) ────
            var extractions = await _db.ExtractedClinicalData
                .AsNoTracking()
                .Where(e => e.PatientId == patientId)
                .ToListAsync();

            if (extractions.Count == 0)
            {
                _logger.LogInformation(
                    "No extracted clinical data found for PatientId={PatientId}. " +
                    "Completing with empty entries.", patientId);
                await SetCompletedAsync(profile, patientId);
                return;
            }

            // ── 5. Flatten extractions into clinical input items ──────────
            var inputItems = FlattenExtractions(extractions);

            // ── 6. Call Gemini for de-duplication (AIR-004) ───────────────
            var deduplicationResult = await _gemini.CallDeduplicationAsync(inputItems);

            // ── 7. Replace old MergedClinicalEntry records for patient ─────
            var oldEntries = await _db.MergedClinicalEntries
                .Where(m => m.PatientId == patientId)
                .ToListAsync();

            if (oldEntries.Count > 0)
            {
                _db.MergedClinicalEntries.RemoveRange(oldEntries);
            }

            // ── 8. Persist canonical entries (EF auto-encrypts PHI) ───────
            var now      = DateTimeOffset.UtcNow;
            var sections = deduplicationResult.Sections;

            AddEntries(_db, patientId, "Vitals",       sections.Vitals,       now);
            AddEntries(_db, patientId, "Medications",  sections.Medications,  now);
            AddEntries(_db, patientId, "Diagnoses",    sections.Diagnoses,    now);
            AddEntries(_db, patientId, "VisitHistory", sections.VisitHistory, now);

            // ── 9. Transition → Completed ─────────────────────────────────
            await SetCompletedAsync(profile, patientId);
        }
        catch (SchemaVersionMismatchException ex)
        {
            _logger.LogError(ex,
                "Schema version mismatch during de-duplication for PatientId={PatientId}.", patientId);
            await SetFailedAsync(profile, ex.Message);
        }
        catch (Exception ex)
        {
            var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;

            _logger.LogError(ex,
                "De-duplication attempt {Attempt}/{MaxAttempts} failed. PatientId={PatientId}.",
                retryCount + 1, MaxRetryAttempts + 1, patientId);

            if (retryCount >= MaxRetryAttempts)
            {
                await SetFailedAsync(profile, ex.Message);
            }
            else
            {
                // Revert to Pending so the retry attempt re-enters the pipeline.
                profile.DeduplicationStatus = "Pending";
                await _uow.SaveChangesAsync();
            }

            throw;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Flattens each <see cref="ExtractedClinicalData"/> record's JSON into a list of
    /// <see cref="ClinicalInputItem"/> values. PHI values are decrypted by EF Core
    /// before this method is called — they must not be logged.
    /// </summary>
    private static List<ClinicalInputItem> FlattenExtractions(
        IReadOnlyList<ExtractedClinicalData> extractions)
    {
        var items = new List<ClinicalInputItem>();

        foreach (var extraction in extractions)
        {
            ClinicalExtractionResult? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<ClinicalExtractionResult>(
                    extraction.EncryptedExtractedJson, JsonOpts);
            }
            catch (JsonException)
            {
                // Corrupt/incomplete extraction — skip silently.
                parsed = null;
            }

            if (parsed is null) continue;

            var docId = extraction.DocumentId.ToString();

            // Vitals
            if (parsed.Vitals is { } v)
            {
                if (!string.IsNullOrWhiteSpace(v.BloodPressure))
                    items.Add(new ClinicalInputItem { DocumentId = docId, SectionType = "Vitals", Label = "Blood pressure", Value = v.BloodPressure });
                if (v.HeartRate.HasValue)
                    items.Add(new ClinicalInputItem { DocumentId = docId, SectionType = "Vitals", Label = "Heart rate", Value = $"{v.HeartRate} bpm" });
                if (v.Weight.HasValue)
                    items.Add(new ClinicalInputItem { DocumentId = docId, SectionType = "Vitals", Label = "Weight", Value = $"{v.Weight} kg" });
                if (v.Temperature.HasValue)
                    items.Add(new ClinicalInputItem { DocumentId = docId, SectionType = "Vitals", Label = "Temperature", Value = $"{v.Temperature} °C" });
            }

            // Medications
            foreach (var med in parsed.Medications)
            {
                if (string.IsNullOrWhiteSpace(med.Name)) continue;
                var value = string.Join(" ", new[] { med.Name, med.Dosage, med.Frequency }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                items.Add(new ClinicalInputItem { DocumentId = docId, SectionType = "Medications", Label = med.Name, Value = value });
            }

            // Diagnoses
            foreach (var dx in parsed.Diagnoses)
            {
                if (string.IsNullOrWhiteSpace(dx.Description) && string.IsNullOrWhiteSpace(dx.IcdCode)) continue;
                var label = dx.Description ?? dx.IcdCode ?? string.Empty;
                var value = string.IsNullOrWhiteSpace(dx.IcdCode) ? label : $"{dx.IcdCode} — {dx.Description}";
                items.Add(new ClinicalInputItem { DocumentId = docId, SectionType = "Diagnoses", Label = label, Value = value });
            }
        }

        return items;
    }

    private static void AddEntries(
        AppDbContext db,
        Guid patientId,
        string sectionType,
        IEnumerable<CanonicalEntry> entries,
        DateTimeOffset now)
    {
        foreach (var entry in entries)
        {
            db.MergedClinicalEntries.Add(new MergedClinicalEntry
            {
                PatientId               = patientId,
                SectionType             = sectionType,
                // EF Core EncryptedStringConverter encrypts these on SaveChanges (AC-003)
                EncryptedLabel          = entry.Label,
                EncryptedCanonicalValue = entry.CanonicalValue,
                SourceDocumentIds       = JsonSerializer.Serialize(entry.SourceDocumentIds),
                Confidence              = entry.Confidence,
                IsPhiField              = entry.IsPhiField,
                MergedAt                = now,
            });
        }
    }

    private async Task SetCompletedAsync(PatientProfile360 profile, Guid patientId)
    {
        profile.DeduplicationStatus = "Completed";
        profile.LastUpdatedAt       = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync();

        await _auditLog.LogAsync(
            actorId:           Guid.Empty,
            actorRole:         "System",
            actionType:        "DEDUP_COMPLETED",
            targetEntity:      "PatientProfile360",
            targetId:          patientId,
            metadata:          null,
            cancellationToken: default);

        _logger.LogInformation(
            "Deduplication completed. PatientId={PatientId}.", patientId);
    }

    private async Task SetFailedAsync(PatientProfile360 profile, string reason)
    {
        profile.DeduplicationStatus = "Failed";
        profile.LastUpdatedAt       = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync();

        _logger.LogError(
            "Deduplication failed. PatientId={PatientId}. Reason={Reason}.",
            profile.PatientId, reason);
    }
}
