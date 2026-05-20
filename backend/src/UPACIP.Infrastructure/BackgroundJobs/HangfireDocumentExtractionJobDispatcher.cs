using Hangfire;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire implementation of <see cref="IDocumentExtractionJobDispatcher"/>.
/// Enqueues <see cref="ClinicalDataExtractionJob"/> as a fire-and-forget background
/// job so the HTTP response is returned before the (potentially long-running)
/// AI extraction completes (AC-004: job enqueued within 5 s of upload completion).
/// </summary>
public sealed class HangfireDocumentExtractionJobDispatcher : IDocumentExtractionJobDispatcher
{
    private readonly IBackgroundJobClient _client;

    public HangfireDocumentExtractionJobDispatcher(IBackgroundJobClient client)
        => _client = client;

    /// <inheritdoc />
    public void Dispatch(Guid clinicalDocumentId)
        => _client.Enqueue<ClinicalDataExtractionJob>(
            j => j.ExecuteAsync(clinicalDocumentId, null));
}
