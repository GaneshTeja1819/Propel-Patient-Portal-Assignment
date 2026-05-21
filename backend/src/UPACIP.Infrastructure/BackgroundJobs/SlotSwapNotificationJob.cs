using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Sends post-swap appointment notification and records delivery state.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class SlotSwapNotificationJob
{
    private const string NotificationType = "SlotSwap";

    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SlotSwapNotificationJob> _logger;

    public SlotSwapNotificationJob(
        AppDbContext dbContext,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<SlotSwapNotificationJob> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid appointmentId,
        Guid newSlotId,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("Slot swap notification skipped; appointment {AppointmentId} not found.", appointmentId);
            return;
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(
                n => n.RecipientId == appointment.PatientId
                     && n.Type == NotificationType
                     && n.Title == appointmentId.ToString("D"),
                cancellationToken);

        notification ??= new Domain.Entities.Notification
        {
            RecipientId = appointment.PatientId,
            Type = NotificationType,
            Title = appointmentId.ToString("D"),
            Body = "Queued",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        if (notification.Id == Guid.Empty)
        {
            await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        }

        notification.Body = "Queued";
        notification.SentAt = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var isInvalidEmail = string.IsNullOrWhiteSpace(appointment.Patient.Email) ||
            !appointment.Patient.Email.Contains('@', StringComparison.Ordinal);

        if (isInvalidEmail)
        {
            notification.Body = "Failed";
            notification.SentAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await WriteFailureAuditAsync(
                appointment.PatientId,
                appointmentId,
                "Patient email missing or invalid",
                cancellationToken);

            _logger.LogWarning(
                "Slot swap notification skipped; invalid recipient email for patient {PatientId}.",
                appointment.PatientId);
            return;
        }

        var providerDisplay = BuildProviderDisplayName(appointment.Provider);
        var subject = $"Your appointment has been moved to {appointment.Slot.StartTime:MMM d, yyyy h:mm tt}";
        var body =
            $"Your appointment {appointment.Id} has been moved.\n" +
            $"New date/time: {appointment.Slot.StartTime:yyyy-MM-dd HH:mm} UTC\n" +
            $"Clinic: {providerDisplay}\n" +
            $"Slot ID: {newSlotId}";

        try
        {
            await _emailService.SendAsync(
                appointment.Patient.Email,
                subject,
                body,
                cancellationToken);

            notification.Body = "Sent";
            notification.SentAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var backoffRetryCount = performContext?.GetJobParameter<int>("BackOffRetryCount") ?? 0;
            if (backoffRetryCount >= 3)
            {
                notification.Body = "Failed";
                notification.SentAt = null;
                await _dbContext.SaveChangesAsync(cancellationToken);

                await WriteFailureAuditAsync(
                    appointment.PatientId,
                    appointmentId,
                    ex.GetType().Name,
                    cancellationToken);
            }

            _logger.LogWarning(
                ex,
                "Slot swap notification failed for appointment {AppointmentId}; retry count {RetryCount}.",
                appointmentId,
                backoffRetryCount);

            throw;
        }
    }

    private async Task WriteFailureAuditAsync(
        Guid patientId,
        Guid appointmentId,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.LogAsync(
                actorId: patientId,
                actorRole: "System",
                actionType: "SLOT_SWAP_NOTIFICATION_FAILED",
                targetEntity: "Appointment",
                targetId: appointmentId,
                metadata: $"{{\"reason\":\"{reason}\"}}",
                cancellationToken: cancellationToken);
        }
        catch (Exception auditEx)
        {
            _logger.LogWarning(
                auditEx,
                "Audit log failed for slot swap notification failure on appointment {AppointmentId}.",
                appointmentId);
        }
    }

    private static string BuildProviderDisplayName(Domain.Entities.User provider)
    {
        var fullName = $"{provider.FirstName} {provider.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? provider.Email : fullName;
    }
}
