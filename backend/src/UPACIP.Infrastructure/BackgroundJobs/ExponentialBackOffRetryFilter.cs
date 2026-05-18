using Hangfire;
using Hangfire.States;
using Hangfire.Storage;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Global job filter implementing NFR-008 exponential back-off retry policy.
///
/// Retry schedule (applied to every job that enters <see cref="FailedState"/>):
///   Attempt 1 → retry after  10 s
///   Attempt 2 → retry after  60 s
///   Attempt 3 → retry after 360 s
///   After 3rd failure → job remains in <see cref="FailedState"/> permanently.
///
/// Registered as a global filter in <see cref="HangfireServiceExtensions"/>
/// so the policy applies to all jobs without requiring per-job annotation.
/// </summary>
public sealed class ExponentialBackOffRetryFilter : IElectStateFilter
{
    private const string RetryCountKey = "BackOffRetryCount";

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(360),
    ];

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
            return;

        var retryCount = context.GetJobParameter<int>(RetryCountKey);

        if (retryCount < RetryDelays.Length)
        {
            var delay = RetryDelays[retryCount];
            context.SetJobParameter(RetryCountKey, retryCount + 1);
            context.CandidateState = new ScheduledState(delay)
            {
                Reason = $"Retry {retryCount + 1}/{RetryDelays.Length} — " +
                         $"back-off {delay.TotalSeconds:0} s. " +
                         $"Error: {failedState.Exception?.Message}"
            };
        }
        else
        {
            // All retries exhausted — annotate the Failed state and leave it.
            failedState.Reason =
                $"Permanently failed after {RetryDelays.Length} retries (NFR-008).";
        }
    }
}
