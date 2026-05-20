namespace UPACIP.Application.Interfaces;

/// <summary>
/// Enqueues and cancels Hangfire-scheduled appointment reminder jobs.
/// Abstraction keeps Hangfire dependency inside Infrastructure (US_020, AC-001).
/// </summary>
public interface IReminderJobEnqueuer
{
    /// <summary>
    /// Schedules Email and SMS reminder jobs for <paramref name="appointmentId"/> at each
    /// configured interval before <paramref name="appointmentStartTime"/>.
    /// Returns the Hangfire job IDs that were scheduled.
    /// </summary>
    IReadOnlyList<string> Schedule(Guid appointmentId, DateTimeOffset appointmentStartTime);

    /// <summary>
    /// Cancels all previously scheduled reminder jobs identified by <paramref name="jobIds"/>.
    /// </summary>
    void Cancel(IEnumerable<string> jobIds);
}
