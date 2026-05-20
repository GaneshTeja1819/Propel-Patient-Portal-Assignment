using Hangfire;
using Microsoft.Extensions.Configuration;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire implementation of <see cref="IReminderJobEnqueuer"/>.
/// Keeps Hangfire dependency inside Infrastructure, away from the Application layer (US_020, AC-001).
/// </summary>
internal sealed class HangfireReminderJobEnqueuer : IReminderJobEnqueuer
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IConfiguration _configuration;

    public HangfireReminderJobEnqueuer(
        IBackgroundJobClient backgroundJobClient,
        IConfiguration configuration)
    {
        _backgroundJobClient = backgroundJobClient;
        _configuration       = configuration;
    }

    public IReadOnlyList<string> Schedule(Guid appointmentId, DateTimeOffset appointmentStartTime)
    {
        // Read configurable intervals (hours before appointment); default: [24, 2]
        var intervalsHours = _configuration
            .GetSection("ReminderIntervals:Hours")
            .Get<int[]>() ?? new[] { 24, 2 };

        var jobIds = new List<string>();

        foreach (var hours in intervalsHours)
        {
            var triggerAt = appointmentStartTime.AddHours(-hours);
            if (triggerAt <= DateTimeOffset.UtcNow)
                continue;

            var emailJobId = _backgroundJobClient.Schedule<AppointmentReminderJob>(
                job => job.SendAsync(appointmentId, "Email", null!, CancellationToken.None),
                triggerAt);

            var smsJobId = _backgroundJobClient.Schedule<AppointmentReminderJob>(
                job => job.SendAsync(appointmentId, "SMS", null!, CancellationToken.None),
                triggerAt);

            jobIds.Add(emailJobId);
            jobIds.Add(smsJobId);
        }

        return jobIds;
    }

    public void Cancel(IEnumerable<string> jobIds)
    {
        foreach (var id in jobIds)
        {
            BackgroundJob.Delete(id);
        }
    }
}
