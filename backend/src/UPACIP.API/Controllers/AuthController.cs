using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using UPACIP.Application.Interfaces;

namespace UPACIP.API.Controllers;

/// <summary>
/// Handles user authentication: login, JWT refresh, and logout.
///
/// Cookie strategy (AC-001, AC-003):
/// <list type="bullet">
///   <item><c>__Host-access</c>  — signed JWT (HttpOnly, Secure, SameSite=Strict); read by JWT bearer middleware.</item>
///   <item><c>__Host-session</c> — opaque session ID (HttpOnly, Secure, SameSite=Strict); maps to Redis refresh entry.</item>
/// </list>
/// Neither cookie value is returned in the response body.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private const string AccessCookieName = "__Host-access";
    private const string SessionCookieName = "__Host-session";
    private const int CookieMaxAgeMinutes = 15;

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and issues JWT + session cookies.
    /// Returns HTTP 200 with no sensitive data in the body.
    /// Returns HTTP 401 with a generic message on invalid credentials (OWASP A07).
    /// </summary>
    [HttpPost("login")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });

        SetAuthCookies(result.AccessToken, result.SessionId);
        return Ok(new { message = "Authenticated." });
    }

    /// <summary>
    /// Issues a new JWT using the session cookie.
    /// Returns HTTP 401 when the session has expired.
    /// Returns HTTP 503 with <c>Retry-After: 30</c> when Redis is unreachable (AC-003 edge case).
    /// </summary>
    [HttpPost("refresh")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var sessionId = Request.Cookies[SessionCookieName];
        if (string.IsNullOrWhiteSpace(sessionId))
            return Unauthorized(new { message = "Session not found." });

        try
        {
            var result = await _authService.RefreshAsync(sessionId, cancellationToken);
            if (result is null)
            {
                ClearAuthCookies();
                return Unauthorized(new { message = "Session expired. Please log in again." });
            }

            SetAuthCookies(result.AccessToken, result.SessionId);
            return Ok(new { message = "Token refreshed." });
        }
        catch (RedisConnectionException)
        {
            Response.Headers["Retry-After"] = "30";
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Authentication service temporarily unavailable. Please retry." });
        }
    }

    /// <summary>
    /// Invalidates the server-side session and clears auth cookies.
    /// Always returns HTTP 200 (idempotent — double-logout is safe).
    /// </summary>
    [HttpPost("logout")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = Request.Cookies[SessionCookieName];
        if (!string.IsNullOrWhiteSpace(sessionId))
            await _authService.LogoutAsync(sessionId, cancellationToken);

        ClearAuthCookies();
        return Ok(new { message = "Logged out." });
    }

    // ── Cookie helpers ────────────────────────────────────────────────────────

    private void SetAuthCookies(string accessToken, string sessionId)
    {
        var cookieOptions = BuildCookieOptions(maxAge: TimeSpan.FromMinutes(CookieMaxAgeMinutes));
        Response.Cookies.Append(AccessCookieName, accessToken, cookieOptions);
        Response.Cookies.Append(SessionCookieName, sessionId, cookieOptions);
    }

    private void ClearAuthCookies()
    {
        // MaxAge = 0 tells the browser to delete the cookie immediately.
        var expiredOptions = BuildCookieOptions(maxAge: TimeSpan.Zero);
        Response.Cookies.Append(AccessCookieName, string.Empty, expiredOptions);
        Response.Cookies.Append(SessionCookieName, string.Empty, expiredOptions);
    }

    private static CookieOptions BuildCookieOptions(TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = maxAge,
    };
}

/// <summary>Login request body. Never logged.</summary>
public sealed record LoginRequest(string Email, string Password);
