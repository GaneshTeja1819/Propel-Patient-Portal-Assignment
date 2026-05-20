using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

internal sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        string attachmentName,
        byte[] attachmentContent,
        string attachmentContentType,
        CancellationToken cancellationToken = default)
    {
        var (smtpClient, fromAddress) = CreateClient();
        using (smtpClient)
        {
            using var message = new MailMessage(fromAddress, toEmail, subject, body);
            using var stream = new MemoryStream(attachmentContent);
            using var attachment = new Attachment(stream, attachmentName, attachmentContentType);
            message.Attachments.Add(attachment);

            _logger.LogInformation("Sending email to {Recipient} with attachment {AttachmentName}.", toEmail, attachmentName);
            await smtpClient.SendMailAsync(message, cancellationToken);
        }
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var (smtpClient, fromAddress) = CreateClient();
        using (smtpClient)
        {
            using var message = new MailMessage(fromAddress, toEmail, subject, body);
            _logger.LogInformation("Sending email to {Recipient}.", toEmail);
            await smtpClient.SendMailAsync(message, cancellationToken);
        }
    }

    private (SmtpClient Client, string FromAddress) CreateClient()
    {
        var host = ReadSetting("SMTP_HOST", "Smtp:Host");
        var from = ReadSetting("SMTP_FROM", "Smtp:FromAddress");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMTP settings are missing. Configure SMTP_HOST and SMTP_FROM.");
        }

        var client = new SmtpClient(host)
        {
            Port = ReadIntSetting("SMTP_PORT", "Smtp:Port") ?? 587,
            EnableSsl = ReadBoolSetting("SMTP_ENABLE_SSL", "Smtp:EnableSsl") ?? true,
        };

        var username = ReadSetting("SMTP_USER", "Smtp:Username");
        var password = ReadSetting("SMTP_PASS", "Smtp:Password");
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        return (client, from);
    }

    private string? ReadSetting(string environmentVariable, string configKey)
        => Environment.GetEnvironmentVariable(environmentVariable) ?? _configuration[configKey];

    private int? ReadIntSetting(string environmentVariable, string configKey)
    {
        var value = ReadSetting(environmentVariable, configKey);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private bool? ReadBoolSetting(string environmentVariable, string configKey)
    {
        var value = ReadSetting(environmentVariable, configKey);
        return bool.TryParse(value, out var parsed) ? parsed : null;
    }
}
