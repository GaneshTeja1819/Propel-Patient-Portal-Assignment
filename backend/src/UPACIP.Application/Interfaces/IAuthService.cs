namespace UPACIP.Application.Interfaces;

/// <summary>Result of a successful login or token refresh.</summary>
/// <param name="AccessToken">Signed JWT; written to an HttpOnly cookie — never returned in response body.</param>
/// <param name="SessionId">Opaque session identifier; written to an HttpOnly cookie; maps to the server-side refresh token in Redis.</param>
public sealed record AuthResult(string AccessToken, string SessionId);

/// <summary>
/// Orchestrates JWT issuance, refresh, and logout for the authentication pipeline
/// (AC-001 – AC-004).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates <paramref name="email"/> / <paramref name="password"/> against the
    /// User table and, on success, issues a signed JWT and creates a Redis session.
    /// Returns <see langword="null"/> if credentials are invalid (same response as
    /// "user not found" to prevent user enumeration — OWASP A07).
    /// </summary>
    Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Exchanges an existing Redis session for a fresh JWT.
    /// Returns <see langword="null"/> if the session has expired or does not exist.
    /// Throws <see cref="StackExchange.Redis.RedisConnectionException"/> when Redis
    /// is unreachable — callers should map this to HTTP 503 + <c>Retry-After: 30</c>.
    /// </summary>
    Task<AuthResult?> RefreshAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Deletes the Redis session, invalidating any outstanding refresh capability.</summary>
    Task LogoutAsync(string sessionId, CancellationToken ct = default);
}
