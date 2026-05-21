using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Application.Interfaces;

namespace UPACIP.API.Controllers;

/// <summary>
/// Calendar synchronisation endpoints (Patient-scoped).
///
/// AC-001: GET /oauth-url returns provider-specific OAuth authorisation URL.
/// AC-001: POST /callback exchanges auth code, creates calendar event, persists CalendarSync.
/// AC-005: POST /callback without code (denied) returns HTTP 200 without creating CalendarSync record.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/calendar-sync")]
[Authorize(Policy = "PatientPolicy")]
public sealed class CalendarSyncController : ControllerBase
{
    private readonly ICalendarSyncService _calendarSyncService;
    private readonly ILogger<CalendarSyncController> _logger;

    public CalendarSyncController(
        ICalendarSyncService calendarSyncService,
        ILogger<CalendarSyncController> logger)
    {
        _calendarSyncService = calendarSyncService;
        _logger              = logger;
    }

    /// <summary>
    /// Returns the OAuth authorisation URL for the requested calendar provider.
    /// </summary>
    /// <param name="provider">Calendar provider: "Google" or "Outlook".</param>
    /// <param name="redirectUri">The URI to redirect to after OAuth consent.</param>
    [HttpGet("oauth-url")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(OAuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetOAuthUrl(
        [FromQuery] string provider,
        [FromQuery] string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { error = "provider and redirectUri are required." });

        if (provider != "Google" && provider != "Outlook")
            return BadRequest(new { error = "provider must be 'Google' or 'Outlook'." });

        // Validate redirectUri is a well-formed absolute URI (OWASP A10 SSRF guard)
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
            return BadRequest(new { error = "redirectUri must be a valid absolute URI." });

        var url = _calendarSyncService.GetOAuthUrl(provider, redirectUri);
        return Ok(new OAuthUrlResponse(url));
    }

    /// <summary>
    /// Exchanges the OAuth authorisation code for tokens, creates a calendar event,
    /// and persists the CalendarSync record.
    /// Returns HTTP 200 (no record created) when consent was denied (code is absent).
    /// </summary>
    [HttpPost("callback")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(CalendarSyncCallbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Callback(
        [FromBody] CalendarSyncCallbackRequest request,
        CancellationToken cancellationToken)
    {
        // AC-005: denied OAuth — return 200 without creating CalendarSync record
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            _logger.LogInformation("Calendar OAuth consent denied by user; no CalendarSync record created.");
            return Ok(new CalendarSyncCallbackResponse(false, "Calendar sync is optional — you can enable it later in Profile Settings."));
        }

        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.AppointmentId))
            return BadRequest(new { error = "provider and appointmentId are required." });

        if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            return BadRequest(new { error = "appointmentId must be a valid GUID." });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("UserId claim is missing."));

        try
        {
            await _calendarSyncService.CreateEventAsync(
                userId, appointmentId, request.Provider, request.Code, cancellationToken);

            return Ok(new CalendarSyncCallbackResponse(true, "Calendar connected ✓"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Calendar sync failed for appointment {AppointmentId} provider {Provider}.",
                appointmentId, request.Provider);

            // AC-004: sync failure is non-blocking; return 200 so frontend shows amber Toast
            return Ok(new CalendarSyncCallbackResponse(false, "Calendar sync failed — your appointment is still confirmed."));
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public sealed record OAuthUrlResponse(string Url);

public sealed record CalendarSyncCallbackRequest(
    string? Code,
    string? Provider,
    string? AppointmentId);

public sealed record CalendarSyncCallbackResponse(bool Synced, string Message);
