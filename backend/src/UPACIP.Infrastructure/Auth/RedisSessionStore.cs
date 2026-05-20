using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace UPACIP.Infrastructure.Auth;

/// <summary>
/// Server-side session store backed by Redis.
///
/// Key format : <c>session:{sessionId}</c>
/// Value      : JSON-serialised <see cref="SessionData"/>
/// TTL        : 15 minutes, sliding — reset on every successful refresh (AC-003, AC-004).
///
/// Never exposes the stored value to the client; only the opaque
/// <paramref name="sessionId"/> travels in an HttpOnly cookie.
/// </summary>
internal sealed class RedisSessionStore
{
    private const string KeyPrefix = "session:";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(15);

    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly ILogger<RedisSessionStore> _logger;

    public RedisSessionStore(IConnectionMultiplexer? multiplexer, ILogger<RedisSessionStore> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    /// <summary>Stores session data and returns a new opaque session ID.</summary>
    public async Task<string> CreateAsync(SessionData data, CancellationToken ct = default)
    {
        if (_multiplexer is null)
        {
            _logger.LogWarning("Redis is not configured — refresh tokens are disabled. Set Redis__ConnectionString for full session support.");
            return GenerateSessionId(); // Dummy session; refresh will fail gracefully.
        }

        var sessionId = GenerateSessionId();
        var json = JsonSerializer.Serialize(data);
        await _multiplexer.GetDatabase()
            .StringSetAsync(KeyPrefix + sessionId, json, SessionTtl);

        return sessionId;
    }

    /// <summary>
    /// Returns the session data and resets the TTL (sliding window).
    /// Returns <see langword="null"/> when the key is absent or expired.
    /// </summary>
    public async Task<SessionData?> GetAndExtendAsync(string sessionId, CancellationToken ct = default)
    {
        RequireMultiplexer();

        var db = _multiplexer!.GetDatabase();
        var key = KeyPrefix + sessionId;

        var value = await db.StringGetAsync(key);
        if (!value.HasValue)
            return null;

        // Reset the TTL so active users never get logged out mid-session (sliding window).
        await db.KeyExpireAsync(key, SessionTtl);

        try
        {
            return JsonSerializer.Deserialize<SessionData>(value.ToString());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not deserialize session data for key {Key}.", key);
            return null;
        }
    }

    /// <summary>Deletes the session key, invalidating any pending refresh.</summary>
    public async Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        if (_multiplexer is null) return; // Best-effort on logout; no need to throw.
        await _multiplexer.GetDatabase().KeyDeleteAsync(KeyPrefix + sessionId);
    }

    private void RequireMultiplexer()
    {
        if (_multiplexer is null)
            throw new InvalidOperationException(
                "Redis is not configured. Authentication sessions require a Redis connection. " +
                "Set the Redis__ConnectionString environment variable.");
    }

    private static string GenerateSessionId()
    {
        // 32 cryptographically random bytes → 64-char hex — unguessable session identifier.
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>Session data stored in Redis — sufficient to re-issue a JWT without a DB round-trip.</summary>
internal sealed record SessionData(Guid UserId, string Role, string Email);
