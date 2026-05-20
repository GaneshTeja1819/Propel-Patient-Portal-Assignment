using Hangfire;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

internal sealed class HangfireWaitlistNotificationJobEnqueuer : IWaitlistNotificationJobEnqueuer
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireWaitlistNotificationJobEnqueuer(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid slotId)
    {
        _backgroundJobClient.Enqueue<WaitlistNotificationJob>(
            job => job.ExecuteAsync(slotId, CancellationToken.None));
    }
}
