namespace UPACIP.Application.Interfaces;

/// <summary>
/// Sends outbound email messages with optional binary attachment payloads.
/// </summary>
public interface IEmailService
{
    Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);

    Task SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        string attachmentName,
        byte[] attachmentContent,
        string attachmentContentType,
        CancellationToken cancellationToken = default);
}
