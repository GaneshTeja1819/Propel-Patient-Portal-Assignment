using Hangfire;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

internal sealed class HangfireAccountLockoutNotifier : IAccountLockoutNotifier
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireAccountLockoutNotifier(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(string email, DateTimeOffset lockUntilUtc)
    {
        _backgroundJobClient.Enqueue<AccountLockoutNotificationJob>(
            job => job.SendLockoutEmailAsync(email, lockUntilUtc));
    }
}
