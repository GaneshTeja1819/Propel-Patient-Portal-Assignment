using Hangfire;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire implementation of <see cref="IPdfConfirmationJobEnqueuer"/>.
/// Keeps Hangfire dependency inside Infrastructure, away from the Application layer.
/// </summary>
internal sealed class HangfirePdfConfirmationJobEnqueuer : IPdfConfirmationJobEnqueuer
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfirePdfConfirmationJobEnqueuer(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid appointmentId, bool isReschedule = false)
    {
        var args = new GeneratePdfJobArgs(appointmentId, isReschedule);
        _backgroundJobClient.Enqueue<GeneratePdfConfirmationJob>(
            job => job.GenerateAsync(args, null!, CancellationToken.None));
    }
}
