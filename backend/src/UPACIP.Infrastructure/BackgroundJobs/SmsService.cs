using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// SMS gateway adapter using a configurable free-tier provider (TextBelt default).
/// Credentials are read from environment variables — never hardcoded (OWASP A02).
///
/// Supported env vars:
///   SMS_API_URL   — gateway base URL (default: https://textbelt.com/text)
///   SMS_API_KEY   — gateway API key (required)
/// </summary>
internal sealed class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _httpClient    = httpClient;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task SendAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("SMS_API_KEY")
            ?? _configuration["Sms:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SMS_API_KEY is not configured; SMS delivery skipped.");
            return;
        }

        var apiUrl = Environment.GetEnvironmentVariable("SMS_API_URL")
            ?? _configuration["Sms:ApiUrl"]
            ?? "https://textbelt.com/text";

        var payload = new Dictionary<string, string>
        {
            ["phone"]   = toPhone,
            ["message"] = message,
            ["key"]     = apiKey,
        };

        _logger.LogInformation("Sending SMS to {Phone}.", toPhone);

        var response = await _httpClient.PostAsync(apiUrl, new FormUrlEncodedContent(payload), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"SMS gateway returned {(int)response.StatusCode}: {body}");
        }

        _logger.LogInformation("SMS dispatched to {Phone}.", toPhone);
    }
}
