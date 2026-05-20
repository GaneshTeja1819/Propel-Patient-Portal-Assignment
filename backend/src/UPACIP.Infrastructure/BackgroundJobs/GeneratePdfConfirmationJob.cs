using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.Documents;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.BackgroundJobs;

public sealed record GeneratePdfJobArgs(Guid AppointmentId, bool IsReschedule);

/// <summary>
/// Generates and emails appointment confirmation PDFs.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class GeneratePdfConfirmationJob
{
    private const string NotificationType = "AppointmentConfirmation";
    private const string PdfRenderRetryKey = "PdfRenderRetryCount";

    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<GeneratePdfConfirmationJob> _logger;

    public GeneratePdfConfirmationJob(
        AppDbContext dbContext,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<GeneratePdfConfirmationJob> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task GenerateAsync(
        GeneratePdfJobArgs args,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == args.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("PDF confirmation skipped; appointment {AppointmentId} not found.", args.AppointmentId);
            return;
        }

        var idempotencyKey = BuildIdempotencyKey(args.AppointmentId);

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(
                n => n.RecipientId == appointment.PatientId
                     && n.Type == NotificationType
                     && n.Title == idempotencyKey,
                cancellationToken);

        if (string.Equals(notification?.Body, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "PDF confirmation already sent for appointment {AppointmentId}; idempotent skip.",
                args.AppointmentId);
            return;
        }

        notification ??= new Domain.Entities.Notification
        {
            RecipientId = appointment.PatientId,
            Type = NotificationType,
            Title = idempotencyKey,
            Body = "Queued",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        if (notification.Id == Guid.Empty)
        {
            await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        }

        notification.Body = "Processing";
        await _dbContext.SaveChangesAsync(cancellationToken);

        byte[] pdfBytes;
        try
        {
            pdfBytes = AppointmentPdfTemplate.Generate(new AppointmentPdfTemplateModel(
                AppointmentId: appointment.Id,
                PatientFullName: $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
                AppointmentStart: appointment.Slot.StartTime,
                ProviderName: BuildProviderDisplayName(appointment.Provider),
                GeneratedAtUtc: DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            var renderAttempt = performContext?.GetJobParameter<int>(PdfRenderRetryKey) ?? 0;

            if (renderAttempt < 1)
            {
                performContext?.SetJobParameter(PdfRenderRetryKey, renderAttempt + 1);
                _logger.LogWarning(
                    ex,
                    "PDF render failed for appointment {AppointmentId}; retrying once.",
                    args.AppointmentId);
                throw;
            }

            notification.Body = "Failed";
            notification.SentAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                actorId: appointment.PatientId,
                actorRole: "System",
                actionType: "PDF_GENERATION_FAILED",
                targetEntity: "Appointment",
                targetId: appointment.Id,
                metadata: "{\"reason\":\"QuestPDF render failed after retry\"}",
                cancellationToken: cancellationToken);

            _logger.LogError(
                ex,
                "PDF render failed permanently for appointment {AppointmentId}.",
                args.AppointmentId);
            return;
        }

        var subject = args.IsReschedule
            ? "Updated Appointment Confirmation"
            : "Appointment Confirmation";

        var body = "Attached is your appointment confirmation PDF.";

        try
        {
            await _emailService.SendWithAttachmentAsync(
                toEmail: appointment.Patient.Email,
                subject: subject,
                body: body,
                attachmentName: $"appointment-{appointment.Id}.pdf",
                attachmentContent: pdfBytes,
                attachmentContentType: "application/pdf",
                cancellationToken: cancellationToken);
        }
        catch (SmtpException ex) when (IsPermanentSmtpFailure(ex))
        {
            notification.Body = "Failed";
            notification.SentAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                ex,
                "Permanent SMTP failure for appointment {AppointmentId}; retries skipped.",
                args.AppointmentId);
            return;
        }
        catch (Exception ex)
        {
            var backoffRetryCount = performContext?.GetJobParameter<int>("BackOffRetryCount") ?? 0;
            if (backoffRetryCount >= 3)
            {
                notification.Body = "Failed";
                notification.SentAt = null;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogWarning(
                ex,
                "SMTP dispatch failed for appointment {AppointmentId}; retry count {RetryCount}.",
                args.AppointmentId,
                backoffRetryCount);

            throw;
        }

        notification.Body = "Sent";
        notification.SentAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string BuildIdempotencyKey(Guid appointmentId)
        => $"appointment_{appointmentId:D}_pdf";

    private static string BuildProviderDisplayName(Domain.Entities.User provider)
    {
        var name = $"{provider.FirstName} {provider.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? provider.Email : name;
    }

    private static bool IsPermanentSmtpFailure(SmtpException exception)
        => exception.StatusCode is
            SmtpStatusCode.MailboxUnavailable or
            SmtpStatusCode.UserNotLocalWillForward or
            SmtpStatusCode.UserNotLocalTryAlternatePath or
            SmtpStatusCode.ExceededStorageAllocation or
            SmtpStatusCode.MailboxNameNotAllowed;
}
