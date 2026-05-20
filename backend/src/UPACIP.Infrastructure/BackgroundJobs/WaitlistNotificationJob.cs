using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Sends notifications to waitlisted patients when a slot is released.
/// </summary>
public sealed class WaitlistNotificationJob
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WaitlistNotificationJob> _logger;

    public WaitlistNotificationJob(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<WaitlistNotificationJob> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid slotId, CancellationToken cancellationToken)
    {
        var slot = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);

        if (slot is null)
        {
            _logger.LogWarning("Waitlist notification skipped; slot {SlotId} not found.", slotId);
            return;
        }

        var waitlistEntries = await _dbContext.WaitlistEntries
            .Include(w => w.Patient)
            .Where(w =>
                w.Status == "Pending" &&
                w.ProviderId == slot.ProviderId &&
                w.RequestedDate.Date == slot.StartTime.Date)
            .ToListAsync(cancellationToken);

        if (waitlistEntries.Count == 0)
        {
            _logger.LogInformation("No waitlist entries for slot {SlotId}; no-op.", slotId);
            return;
        }

        var host = _configuration["Smtp:Host"];
        var from = _configuration["Smtp:FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("SMTP not configured; waitlist notification skipped for slot {SlotId}.", slotId);
            return;
        }

        using var client = new SmtpClient(host)
        {
            Port = _configuration.GetValue<int?>("Smtp:Port") ?? 587,
            EnableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true,
        };

        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        foreach (var waitlistEntry in waitlistEntries)
        {
            if (string.IsNullOrWhiteSpace(waitlistEntry.Patient.Email))
            {
                continue;
            }

            var subject = "Appointment slot now available";
            var body =
                $"A slot is now available on {slot.StartTime:u}. Please sign in to complete booking.";

            try
            {
                using var message = new MailMessage(from, waitlistEntry.Patient.Email, subject, body);
                await client.SendMailAsync(message, cancellationToken);

                waitlistEntry.Status = "Notified";
                waitlistEntry.NotifiedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Waitlist notification delivery failed for waitlist entry {WaitlistEntryId}.",
                    waitlistEntry.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
