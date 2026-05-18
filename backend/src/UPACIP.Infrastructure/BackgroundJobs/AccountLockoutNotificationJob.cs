using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Sends lockout notifications via SMTP when account lockout is triggered.
/// </summary>
public sealed class AccountLockoutNotificationJob
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountLockoutNotificationJob> _logger;

    public AccountLockoutNotificationJob(
        IConfiguration configuration,
        ILogger<AccountLockoutNotificationJob> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendLockoutEmailAsync(string email, DateTimeOffset lockUntilUtc)
    {
        var host = _configuration["Smtp:Host"];
        var from = _configuration["Smtp:FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning(
                "SMTP is not configured; lockout notification skipped for {Email}.",
                email);
            return;
        }

        var subject = "Your account has been temporarily locked";
        var body = $"Your account has been locked until {lockUntilUtc:u} due to failed sign-in attempts.";

        using var client = new SmtpClient(host)
        {
            Port = _configuration.GetValue<int?>("Smtp:Port") ?? 587,
            EnableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true,
        };

        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage(from, email, subject, body);
        await client.SendMailAsync(message);
    }
}
