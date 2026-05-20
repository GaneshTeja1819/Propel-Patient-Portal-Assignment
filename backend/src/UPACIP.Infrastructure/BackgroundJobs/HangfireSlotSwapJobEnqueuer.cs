using Hangfire;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

internal sealed class HangfireSlotSwapJobEnqueuer : ISlotSwapJobEnqueuer
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireSlotSwapJobEnqueuer(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid releasedSlotId)
    {
        _backgroundJobClient.Enqueue<SlotSwapJob>(
            job => job.ExecuteAsync(releasedSlotId, CancellationToken.None));
    }
}
