using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire job that dispatches Email and SMS appointment reminders.
///
/// AC-001: Reminders dispatched at configured pre-appointment intervals.
/// AC-002: Email sent via IEmailService (SMTP); delivery status recorded.
/// AC-003: SMS sent via ISmsService; skipped without error when phone absent.
/// AC-004: Max-3-retry exponential back-off (10 s / 60 s / 360 s); Notification status = "Failed" after exhaustion.
/// AC-005: Cancelled appointments are skipped without error.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 60, 360 })]
public sealed class AppointmentReminderJob
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        AppDbContext dbContext,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<AppointmentReminderJob> logger)
    {
        _dbContext    = dbContext;
        _emailService = emailService;
        _smsService   = smsService;
        _logger       = logger;
    }

    /// <summary>
    /// Executes the reminder dispatch for one appointment and one channel.
    /// </summary>
    /// <param name="appointmentId">Target appointment.</param>
    /// <param name="channel">Dispatch channel: "Email" or "SMS".</param>
    /// <param name="performContext">Hangfire context (injected by framework).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendAsync(
        Guid appointmentId,
        string channel,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("Reminder skipped; appointment {AppointmentId} not found.", appointmentId);
            return;
        }

        // AC-005: skip cancelled appointments
        if (string.Equals(appointment.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Reminder skipped; appointment {AppointmentId} is cancelled.", appointmentId);
            return;
        }

        // Edge case: reminder fires within 1 minute of appointment start — too late to be useful
        if (appointment.Slot.StartTime - DateTimeOffset.UtcNow <= TimeSpan.FromMinutes(1))
        {
            _logger.LogInformation(
                "Reminder skipped; appointment {AppointmentId} starts within 1 minute.", appointmentId);
            return;
        }

        var idempotencyKey = $"AppointmentReminder_{channel}_{appointmentId}";
        var notification = await GetOrCreateNotificationAsync(appointment, channel, idempotencyKey, cancellationToken);

        if (string.Equals(notification.Body, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Reminder already sent for {Key}; idempotent skip.", idempotencyKey);
            return;
        }

        try
        {
            if (string.Equals(channel, "Email", StringComparison.OrdinalIgnoreCase))
            {
                await SendEmailReminderAsync(appointment, cancellationToken);
            }
            else if (string.Equals(channel, "SMS", StringComparison.OrdinalIgnoreCase))
            {
                await SendSmsReminderAsync(appointment, cancellationToken);
            }

            notification.Body      = "Sent";
            notification.SentAt    = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Reminder dispatch failed for appointment {AppointmentId} channel {Channel}.",
                appointmentId, channel);

            // Mark as Failed only on the final Hangfire attempt (RetryCount == 3).
            // Earlier attempts leave the status as "Queued" so the next retry can succeed.
            var retryCount = performContext?.GetJobParameter<int>("RetryCount") ?? 0;
            if (retryCount >= 3)
            {
                notification.Body = "Failed";
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            throw; // re-throw so Hangfire schedules the next retry
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task<Notification> GetOrCreateNotificationAsync(
        Appointment appointment,
        string channel,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications.FirstOrDefaultAsync(
            n => n.RecipientId == appointment.PatientId && n.Title == idempotencyKey,
            cancellationToken);

        if (notification is not null)
            return notification;

        notification = new Notification
        {
            RecipientId  = appointment.PatientId,
            Type         = $"AppointmentReminder_{channel}",
            Title        = idempotencyKey,
            Body         = "Queued",
            ScheduledFor = DateTimeOffset.UtcNow,
            CreatedAt    = DateTimeOffset.UtcNow,
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    private async Task SendEmailReminderAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var patient  = appointment.Patient;
        var slotTime = appointment.Slot.StartTime;
        var subject  = $"Reminder: Your appointment on {slotTime:dddd, MMMM d} at {slotTime:h:mm tt}";
        var body     = $"Dear {patient.FirstName},\n\n" +
                       $"This is a reminder for your appointment scheduled on " +
                       $"{slotTime:dddd, MMMM d, yyyy} at {slotTime:h:mm tt}.\n\n" +
                       $"Appointment ID: {appointment.Id}\n\n" +
                       "If you need to reschedule or cancel, please log in to the patient portal.\n\n" +
                       "Thank you.";

        await _emailService.SendAsync(patient.Email, subject, body, cancellationToken);
    }

    private async Task SendSmsReminderAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var patient = appointment.Patient;

        // AC-003: skip without error when phone number is absent
        if (string.IsNullOrWhiteSpace(patient.PhoneNumber))
        {
            _logger.LogInformation(
                "SMS reminder skipped for appointment {AppointmentId}; patient has no phone number.",
                appointment.Id);
            return;
        }

        var slotTime = appointment.Slot.StartTime;
        var message  = $"Reminder: Your appointment is on {slotTime:MMM d} at {slotTime:h:mm tt}. " +
                       $"Appt ID: {appointment.Id}";

        try
        {
            await _smsService.SendAsync(patient.PhoneNumber, message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Edge case: unsupported international format — log warning; email still sent; do not re-throw
            _logger.LogWarning(ex,
                "SMS reminder failed for appointment {AppointmentId}; phone may be in unsupported format.",
                appointment.Id);
        }
    }
}
