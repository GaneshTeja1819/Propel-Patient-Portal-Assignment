namespace UPACIP.Application.Interfaces;

/// <summary>
/// Sends outbound SMS messages via a configured free-tier SMS gateway.
/// Credentials are loaded from environment variables — never hardcoded (OWASP A02).
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS to <paramref name="toPhone"/>.
    /// Returns normally when delivery is accepted by the gateway or the phone number is absent.
    /// Throws on gateway communication errors so Hangfire can retry.
    /// </summary>
    Task SendAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
