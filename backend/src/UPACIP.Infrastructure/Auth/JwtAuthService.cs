using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.Auth;

/// <summary>
/// Issues and validates JWTs for the UPACIP authentication pipeline (AC-001–AC-004).
///
/// Signing key : <c>JWT_SIGNING_KEY</c> environment variable; must be at least 32
///               characters (256 bits) for HMAC-SHA256. Constructor throws
///               <see cref="InvalidOperationException"/> at startup if absent or too short.
///
/// Token expiry: exactly 15 minutes from issuance (<c>ClockSkew = Zero</c> on the
///               validation side ensures T+15min+1s returns HTTP 401 — AC-002).
///
/// Claim scheme: <c>sub</c> = userId, <c>role</c> = user role, <c>email</c> = email,
///               <c>jti</c> = unique token ID. Short names keep payloads compact and
///               avoid URI-form ClaimTypes leaking implementation details.
/// </summary>
internal sealed class JwtAuthService : IAuthService
{
    private const int TokenExpiryMinutes = 15;

    private readonly AppDbContext _db;
    private readonly RedisSessionStore _sessionStore;
    private readonly ILogger<JwtAuthService> _logger;
    private readonly SigningCredentials _signingCredentials;

    public JwtAuthService(
        AppDbContext db,
        RedisSessionStore sessionStore,
        ILogger<JwtAuthService> logger)
    {
        _db = db;
        _sessionStore = sessionStore;
        _logger = logger;

        var rawKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
            ?? throw new InvalidOperationException(
                "JWT_SIGNING_KEY environment variable is not set. " +
                "Provide a string of at least 32 characters before starting the application.");

        if (rawKey.Length < 32)
            throw new InvalidOperationException(
                $"JWT_SIGNING_KEY must be at least 32 characters (256 bits). " +
                $"Current length: {rawKey.Length}.");

        var keyBytes = Encoding.UTF8.GetBytes(rawKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        // Normalise email to prevent case-sensitivity enumeration
        var normalisedEmail = email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalisedEmail && u.IsActive, ct);

        // Constant-time path: always verify even when user not found to prevent timing attacks.
        var dummyHash = "$2a$11$InvalidHashUsedToPreventTimingAttack/////////////////////";
        var hashToVerify = user?.PasswordHash ?? dummyHash;
        var passwordValid = BCrypt.Net.BCrypt.Verify(password, hashToVerify);

        if (user is null || !passwordValid)
        {
            _logger.LogWarning("Failed login attempt for email {Email}.", normalisedEmail);
            return null;
        }

        var accessToken = BuildJwt(user.Id, user.Role, user.Email);
        var sessionId = await _sessionStore.CreateAsync(new SessionData(user.Id, user.Role, user.Email), ct);

        return new AuthResult(accessToken, sessionId);
    }

    /// <inheritdoc />
    public async Task<AuthResult?> RefreshAsync(string sessionId, CancellationToken ct = default)
    {
        // RedisConnectionException propagates to the caller (AuthController maps it to 503).
        var session = await _sessionStore.GetAndExtendAsync(sessionId, ct);
        if (session is null)
            return null;

        var newAccessToken = BuildJwt(session.UserId, session.Role, session.Email);
        // Issue a new session ID (token rotation — prevents session fixation attacks)
        await _sessionStore.DeleteAsync(sessionId, ct);
        var newSessionId = await _sessionStore.CreateAsync(session, ct);

        return new AuthResult(newAccessToken, newSessionId);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string sessionId, CancellationToken ct = default)
    {
        await _sessionStore.DeleteAsync(sessionId, ct);
    }

    private string BuildJwt(Guid userId, string role, string email)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(TokenExpiryMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
