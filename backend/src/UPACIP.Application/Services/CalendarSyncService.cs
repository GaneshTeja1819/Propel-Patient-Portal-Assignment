using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Application.Services;

/// <summary>
/// Manages Google Calendar and Outlook calendar events for patient appointments.
///
/// AC-001: OAuth consent → event created; CalendarSync record persisted with SyncStatus="Synced".
/// AC-002: Appointment rescheduled → calendar event updated; CalendarSync.LastSyncedAt updated.
/// AC-003: Appointment cancelled → calendar event deleted; CalendarSync.SyncStatus="Deleted".
/// AC-004: API failure → non-blocking; CalendarSync.SyncStatus="Failed"; retry via Hangfire.
/// AC-005: Denied consent → no CalendarSync record created.
/// </summary>
public sealed class CalendarSyncService : ICalendarSyncService
{
    private readonly AppDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CalendarSyncService> _logger;

    // Google token/API endpoints
    private const string GoogleTokenUrl  = "https://oauth2.googleapis.com/token";
    private const string GoogleEventsUrl = "https://www.googleapis.com/calendar/v3/calendars/primary/events";

    // Outlook token/API endpoints
    private const string OutlookTokenUrl  = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private const string OutlookEventsUrl = "https://graph.microsoft.com/v1.0/me/events";

    public CalendarSyncService(
        AppDbContext dbContext,
        IEncryptionService encryptionService,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CalendarSyncService> logger)
    {
        _dbContext         = dbContext;
        _encryptionService = encryptionService;
        _httpClient        = httpClient;
        _configuration     = configuration;
        _logger            = logger;
    }

    // ── ICalendarSyncService ──────────────────────────────────────────────

    public string GetOAuthUrl(string provider, string redirectUri)
    {
        return provider switch
        {
            "Google"  => BuildGoogleOAuthUrl(redirectUri),
            "Outlook" => BuildOutlookOAuthUrl(redirectUri),
            _         => throw new ArgumentException($"Unsupported calendar provider: {provider}", nameof(provider)),
        };
    }

    public async Task CreateEventAsync(
        Guid userId,
        Guid appointmentId,
        string provider,
        string authCode,
        CancellationToken cancellationToken = default)
    {
        // Exchange code for tokens
        var (accessToken, refreshToken, expiresAt) = await ExchangeCodeAsync(provider, authCode, cancellationToken);

        var appointment = await _dbContext.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("CalendarSync skipped; appointment {AppointmentId} not found.", appointmentId);
            return;
        }

        // Encrypt tokens before persistence (OWASP A02, NFR-001)
        var encryptedAccess  = _encryptionService.Encrypt(accessToken);
        var encryptedRefresh = _encryptionService.Encrypt(refreshToken);

        string? eventId = null;
        var syncStatus  = "Failed";

        try
        {
            eventId    = await CreateCalendarEventAsync(provider, accessToken, appointment, cancellationToken);
            syncStatus = "Synced";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Calendar event creation failed for appointment {AppointmentId}.", appointmentId);
        }

        var calendarSync = new CalendarSync
        {
            UserId                 = userId,
            AppointmentId          = appointmentId,
            Provider               = provider,
            EncryptedAccessToken   = encryptedAccess,
            EncryptedRefreshToken  = encryptedRefresh,
            ExpiresAt              = expiresAt,
            LastSyncedAt           = DateTimeOffset.UtcNow,
            SyncStatus             = syncStatus,
            CalendarEventId        = eventId,
            IsActive               = true,
        };

        _dbContext.CalendarSyncs.Add(calendarSync);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (syncStatus == "Failed")
            throw new InvalidOperationException($"Calendar event creation failed for appointment {appointmentId}.");
    }

    public async Task UpdateEventAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var syncs = await _dbContext.CalendarSyncs
            .Where(c => c.AppointmentId == appointmentId && c.SyncStatus == "Synced")
            .ToListAsync(cancellationToken);

        if (syncs.Count == 0) return;

        var appointment = await _dbContext.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null) return;

        foreach (var sync in syncs)
        {
            try
            {
                var accessToken = await GetValidAccessTokenAsync(sync, cancellationToken);
                await UpdateCalendarEventAsync(sync.Provider, accessToken, sync.CalendarEventId!, appointment, cancellationToken);
                sync.LastSyncedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Calendar event update failed for appointment {AppointmentId} provider {Provider}.",
                    appointmentId, sync.Provider);
                sync.SyncStatus = "Failed";
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task DeleteEventAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var syncs = await _dbContext.CalendarSyncs
            .Where(c => c.AppointmentId == appointmentId && c.SyncStatus == "Synced")
            .ToListAsync(cancellationToken);

        if (syncs.Count == 0) return;

        foreach (var sync in syncs)
        {
            try
            {
                var accessToken = await GetValidAccessTokenAsync(sync, cancellationToken);
                await DeleteCalendarEventAsync(sync.Provider, accessToken, sync.CalendarEventId!, cancellationToken);
                sync.SyncStatus  = "Deleted";
                sync.LastSyncedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Calendar event delete failed for appointment {AppointmentId} provider {Provider}.",
                    appointmentId, sync.Provider);
                sync.SyncStatus = "Failed";
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    // ── OAuth URL builders ────────────────────────────────────────────────

    private string BuildGoogleOAuthUrl(string redirectUri)
    {
        var clientId = GetRequiredEnvOrConfig("GOOGLE_CLIENT_ID", "Calendar:Google:ClientId");
        var scope    = Uri.EscapeDataString("https://www.googleapis.com/auth/calendar.events");
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&access_type=offline" +
               $"&prompt=consent";
    }

    private string BuildOutlookOAuthUrl(string redirectUri)
    {
        var clientId = GetRequiredEnvOrConfig("OUTLOOK_CLIENT_ID", "Calendar:Outlook:ClientId");
        var scope    = Uri.EscapeDataString("Calendars.ReadWrite offline_access");
        return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
               $"?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={scope}";
    }

    // ── Token exchange ────────────────────────────────────────────────────

    private async Task<(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)> ExchangeCodeAsync(
        string provider, string authCode, CancellationToken cancellationToken)
    {
        var tokenUrl     = provider == "Google" ? GoogleTokenUrl : OutlookTokenUrl;
        var clientId     = GetRequiredEnvOrConfig(
                               provider == "Google" ? "GOOGLE_CLIENT_ID" : "OUTLOOK_CLIENT_ID",
                               provider == "Google" ? "Calendar:Google:ClientId" : "Calendar:Outlook:ClientId");
        var clientSecret = GetRequiredEnvOrConfig(
                               provider == "Google" ? "GOOGLE_CLIENT_SECRET" : "OUTLOOK_CLIENT_SECRET",
                               provider == "Google" ? "Calendar:Google:ClientSecret" : "Calendar:Outlook:ClientSecret");
        var redirectUri  = GetRequiredEnvOrConfig(
                               "CALENDAR_REDIRECT_URI",
                               "Calendar:RedirectUri");

        var payload = new Dictionary<string, string>
        {
            ["code"]          = authCode,
            ["client_id"]     = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"]  = redirectUri,
            ["grant_type"]    = "authorization_code",
        };

        var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(payload), cancellationToken);
        response.EnsureSuccessStatusCode();

        using var tokenJson = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc       = await JsonDocument.ParseAsync(tokenJson, cancellationToken: cancellationToken);
        var root            = doc.RootElement;

        var accessToken  = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString()! : string.Empty;
        var expiresIn    = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;
        var expiresAt    = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        return (accessToken, refreshToken, expiresAt);
    }

    private async Task<string> RefreshAccessTokenAsync(CalendarSync sync, CancellationToken cancellationToken)
    {
        var tokenUrl     = sync.Provider == "Google" ? GoogleTokenUrl : OutlookTokenUrl;
        var clientId     = GetRequiredEnvOrConfig(
                               sync.Provider == "Google" ? "GOOGLE_CLIENT_ID" : "OUTLOOK_CLIENT_ID",
                               sync.Provider == "Google" ? "Calendar:Google:ClientId" : "Calendar:Outlook:ClientId");
        var clientSecret = GetRequiredEnvOrConfig(
                               sync.Provider == "Google" ? "GOOGLE_CLIENT_SECRET" : "OUTLOOK_CLIENT_SECRET",
                               sync.Provider == "Google" ? "Calendar:Google:ClientSecret" : "Calendar:Outlook:ClientSecret");

        var decryptedRefresh = _encryptionService.Decrypt(sync.EncryptedRefreshToken);

        var payload = new Dictionary<string, string>
        {
            ["refresh_token"] = decryptedRefresh,
            ["client_id"]     = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"]    = "refresh_token",
        };

        var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(payload), cancellationToken);
        response.EnsureSuccessStatusCode();

        using var tokenJson = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc       = await JsonDocument.ParseAsync(tokenJson, cancellationToken: cancellationToken);
        var root            = doc.RootElement;

        var newAccessToken = root.GetProperty("access_token").GetString()!;
        var expiresIn      = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

        sync.EncryptedAccessToken = _encryptionService.Encrypt(newAccessToken);
        sync.ExpiresAt            = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        return newAccessToken;
    }

    private async Task<string> GetValidAccessTokenAsync(CalendarSync sync, CancellationToken cancellationToken)
    {
        // Refresh if within 60 seconds of expiry (edge case: token expires between creation and sync)
        if (sync.ExpiresAt.HasValue && sync.ExpiresAt.Value <= DateTimeOffset.UtcNow.AddSeconds(60))
        {
            _logger.LogInformation("Access token expiring; refreshing for provider {Provider}.", sync.Provider);
            return await RefreshAccessTokenAsync(sync, cancellationToken);
        }

        return _encryptionService.Decrypt(sync.EncryptedAccessToken);
    }

    // ── Calendar event CRUD ────────────────────────────────────────────────

    private async Task<string> CreateCalendarEventAsync(
        string provider, string accessToken, Appointment appointment, CancellationToken cancellationToken)
    {
        var eventBody = BuildEventPayload(appointment);

        if (provider == "Google")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GoogleEventsUrl)
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
                Content = new StringContent(JsonSerializer.Serialize(eventBody), Encoding.UTF8, "application/json"),
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc    = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return doc.RootElement.GetProperty("id").GetString()!;
        }
        else // Outlook
        {
            var outlookBody = BuildOutlookEventPayload(appointment);
            var request = new HttpRequestMessage(HttpMethod.Post, OutlookEventsUrl)
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
                Content = new StringContent(JsonSerializer.Serialize(outlookBody), Encoding.UTF8, "application/json"),
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc    = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return doc.RootElement.GetProperty("id").GetString()!;
        }
    }

    private async Task UpdateCalendarEventAsync(
        string provider, string accessToken, string eventId, Appointment appointment, CancellationToken cancellationToken)
    {
        if (provider == "Google")
        {
            var eventBody = BuildEventPayload(appointment);
            var url       = $"{GoogleEventsUrl}/{Uri.EscapeDataString(eventId)}";
            var request   = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
                Content = new StringContent(JsonSerializer.Serialize(eventBody), Encoding.UTF8, "application/json"),
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        else // Outlook
        {
            var outlookBody = BuildOutlookEventPayload(appointment);
            var url         = $"{OutlookEventsUrl}/{Uri.EscapeDataString(eventId)}";
            var request     = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
                Content = new StringContent(JsonSerializer.Serialize(outlookBody), Encoding.UTF8, "application/json"),
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task DeleteCalendarEventAsync(
        string provider, string accessToken, string eventId, CancellationToken cancellationToken)
    {
        var url = provider == "Google"
            ? $"{GoogleEventsUrl}/{Uri.EscapeDataString(eventId)}"
            : $"{OutlookEventsUrl}/{Uri.EscapeDataString(eventId)}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        // 404 on delete is acceptable — event may have already been removed
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    // ── Event payload builders ────────────────────────────────────────────

    private static object BuildEventPayload(Appointment appointment)
    {
        var start = appointment.Slot.StartTime;
        var end   = start.AddMinutes(30);

        return new
        {
            summary     = "Medical Appointment",
            description = $"Appointment ID: {appointment.Id}",
            start       = new { dateTime = start.ToString("o"), timeZone = "UTC" },
            end         = new { dateTime = end.ToString("o"),   timeZone = "UTC" },
        };
    }

    private static object BuildOutlookEventPayload(Appointment appointment)
    {
        var start = appointment.Slot.StartTime;
        var end   = start.AddMinutes(30);

        return new
        {
            subject = "Medical Appointment",
            body    = new { contentType = "Text", content = $"Appointment ID: {appointment.Id}" },
            start   = new { dateTime = start.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "UTC" },
            end     = new { dateTime = end.ToString("yyyy-MM-ddTHH:mm:ss"),   timeZone = "UTC" },
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private string GetRequiredEnvOrConfig(string envVar, string configKey)
    {
        var value = Environment.GetEnvironmentVariable(envVar)
            ?? _configuration[configKey];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Required configuration '{envVar}' / '{configKey}' is not set (OWASP A02).");

        return value;
    }
}
