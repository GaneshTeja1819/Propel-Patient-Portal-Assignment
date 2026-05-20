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
/// Hangfire background job that drives the full AI clinical data extraction
/// pipeline for a single <c>ClinicalDocument</c> (US_026, AC-001 through AC-005).
///
/// Pipeline stages:
/// <list type="number">
///   <item>Transition <c>ExtractionStatus</c>: Pending → Processing.</item>
///   <item>Download PDF bytes from Supabase Storage via <see cref="IDocumentStorageService"/>.</item>
///   <item>Extract plain text via PdfPig (<see cref="IPdfTextExtractor"/>).</item>
///   <item>Invoke Gemini structured extraction (<see cref="GeminiExtractionAdapter"/>).</item>
///   <item>Persist <see cref="ExtractedClinicalData"/>; EF Core encrypts PHI JSON on SaveChanges (AC-002).</item>
///   <item>Transition <c>ExtractionStatus</c>: Processing → Completed.</item>
///   <item>Enqueue <see cref="DeduplicationJob"/> as a downstream fire-and-forget continuation (AC-005).</item>
/// </list>
///
/// <para>
/// <c>AI_INVOCATION</c> audit entries (AC-003, AIR-006/AIR-007) are written automatically
/// by <see cref="GeminiInvocationLogger"/> for every Gemini call — no manual audit
/// writes are required here.
/// </para>
///
/// <para>
/// Retry behaviour: <see cref="AutomaticRetryAttribute"/> applies two retries at 30 s and
/// 300 s back-off (AC-004). When all retry attempts are exhausted the job sets
/// <c>ExtractionStatus = "Failed"</c> and stores the failure reason in
/// <c>ExtractionFailureNote</c> rather than re-throwing, so Hangfire marks the job
/// as Succeeded rather than leaving it in the dead-letter queue.
/// </para>
///
/// <para>
/// <see cref="SchemaVersionMismatchException"/> is non-retriable (edge case: schema
/// evolution) — caught immediately, status set to Failed, and the exception is swallowed.
/// </para>
/// </summary>
[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 300 })]
internal sealed class ClinicalDataExtractionJob
{
    /// <summary>Must match the <c>Attempts</c> value in <see cref="AutomaticRetryAttribute"/> above.</summary>
    private const int MaxRetryAttempts = 2;

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IDocumentStorageService _storage;
    private readonly GeminiExtractionAdapter _gemini;
    private readonly IUnitOfWork _uow;
    private readonly IBackgroundJobClient _client;
    private readonly ILogger<ClinicalDataExtractionJob> _logger;

    public ClinicalDataExtractionJob(
        AppDbContext db,
        IPdfTextExtractor pdfExtractor,
        IDocumentStorageService storage,
        GeminiExtractionAdapter gemini,
        IUnitOfWork uow,
        IBackgroundJobClient client,
        ILogger<ClinicalDataExtractionJob> logger)
    {
        _db           = db;
        _pdfExtractor = pdfExtractor;
        _storage      = storage;
        _gemini       = gemini;
        _uow          = uow;
        _client       = client;
        _logger       = logger;
    }

    /// <summary>
    /// Entry point invoked by Hangfire. See class summary for pipeline description.
    /// <para>
    /// <paramref name="context"/> is injected automatically by Hangfire; it is never
    /// serialised as part of the job arguments.
    /// </para>
    /// </summary>
    public async Task ExecuteAsync(Guid clinicalDocumentId, PerformContext? context = null)
    {
        _logger.LogInformation(
            "Clinical extraction started. DocumentId={DocumentId}.", clinicalDocumentId);

        var document = await _db.ClinicalDocuments
            .FirstOrDefaultAsync(d => d.Id == clinicalDocumentId);

        if (document is null)
        {
            // Document deleted between enqueue and execution — do not retry.
            _logger.LogError(
                "ClinicalDocument {DocumentId} not found — job cannot proceed.", clinicalDocumentId);
            return;
        }

        // Transition: Pending → Processing (AC-001)
        document.ExtractionStatus = "Processing";
        await _uow.SaveChangesAsync();

        try
        {
            // Stage 1: download PDF.
            // StoragePath is stored encrypted; EF Core EncryptedStringConverter decrypts it on load.
            var pdfBytes = await _storage.DownloadAsync(document.StoragePath);

            // Stage 2: extract plain text via PdfPig.
            var text = _pdfExtractor.ExtractText(pdfBytes, clinicalDocumentId);

            if (string.IsNullOrEmpty(text))
            {
                // Edge case AC-001: image-only PDF — no text to extract.
                await SetFailedAsync(document, "No text extracted from document.");
                return;
            }

            // Stage 3: structured Gemini extraction.
            // GeminiInvocationLogger writes AI_INVOCATION audit entry automatically (AC-003, AIR-006).
            var extraction = await _gemini.CallExtractionAsync(text);

            // Stage 4: persist ExtractedClinicalData.
            // EncryptedExtractedJson is stored as plaintext here; the EF Core
            // EncryptedStringConverter encrypts it with AES-256-GCM on SaveChanges (AC-002).
            var extractedJson = JsonSerializer.Serialize(extraction, JsonOpts);

            var record = new ExtractedClinicalData
            {
                DocumentId             = document.Id,
                PatientId              = document.PatientId,
                EncryptedExtractedJson = extractedJson,
                ExtractionModel        = "gemini-1.5-pro",
                ExtractedAt            = DateTimeOffset.UtcNow,
            };

            await _db.ExtractedClinicalData.AddAsync(record);
            document.ExtractionStatus = "Completed";
            await _uow.SaveChangesAsync();

            // Enqueue DeduplicationJob as a downstream fire-and-forget continuation (AC-005).
            // The extraction job does NOT await this — it returns immediately after enqueuing.
            _client.Enqueue<DeduplicationJob>(j => j.ExecuteAsync(document.PatientId, null));

            // Enqueue CodeSuggestionJob so ranked ICD-10/CPT candidates are generated
            // automatically after extraction (US_029, AC-001 auto-trigger path).
            _client.Enqueue<CodeSuggestionJob>(j => j.ExecuteAsync(record.Id, null));

            _logger.LogInformation(
                "Clinical extraction completed. DocumentId={DocumentId}.", clinicalDocumentId);
        }
        catch (SchemaVersionMismatchException ex)
        {
            // Non-retriable: schema mismatch must not loop through back-off retries.
            // Set Failed and swallow so Hangfire marks the job Succeeded (edge case: schema evolution).
            _logger.LogError(ex,
                "Schema version mismatch for document {DocumentId}.", clinicalDocumentId);
            await SetFailedAsync(document, ex.Message);
        }
        catch (Exception ex)
        {
            // Read the retry counter that AutomaticRetryAttribute tracks (AC-004).
            // RetryCount == 0 on first execution, increments after each failure.
            // When RetryCount >= MaxRetryAttempts this is the final attempt.
            var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;

            _logger.LogError(ex,
                "Extraction attempt {Attempt}/{MaxAttempts} failed. DocumentId={DocumentId}.",
                retryCount + 1, MaxRetryAttempts + 1, clinicalDocumentId);

            if (retryCount >= MaxRetryAttempts)
            {
                // All retries exhausted — mark Failed so the UI can surface the retry CTA (UXR-603).
                // Do NOT re-throw; Hangfire marks the job Succeeded rather than leaving it in the
                // dead-letter queue, which would otherwise hide it from operational monitoring.
                await SetFailedAsync(document, $"{ex.GetType().Name}: {ex.Message}");
                return;
            }

            // Not the final attempt — re-throw so [AutomaticRetry] schedules the next retry.
            throw;
        }
    }

    private async Task SetFailedAsync(ClinicalDocument document, string reason)
    {
        document.ExtractionStatus    = "Failed";
        document.ExtractionFailureNote = reason;
        await _uow.SaveChangesAsync();
        _logger.LogWarning(
            "Extraction set to Failed. DocumentId={DocumentId}, Reason={Reason}.",
            document.Id, reason);
    }
}

