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
/// A secondary set <c>user-sessions:{userId}</c> maps each userId to the set of
/// active session IDs, enabling O(1) user-level invalidation (e.g., after role change
/// or account deactivation). The set TTL is refreshed on each session creation; stale
/// entries are harmless because deleting an already-expired key is a no-op.
///
/// Never exposes the stored value to the client; only the opaque
/// <paramref name="sessionId"/> travels in an HttpOnly cookie.
/// </summary>
internal sealed class RedisSessionStore
{
    private const string KeyPrefix = "session:";
    private const string UserSessionsPrefix = "user-sessions:";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan UserSessionsSetTtl = TimeSpan.FromHours(24);

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
        RequireMultiplexer();

        var sessionId = GenerateSessionId();
        var json = JsonSerializer.Serialize(data);
        var db = _multiplexer!.GetDatabase();

        await db.StringSetAsync(KeyPrefix + sessionId, json, SessionTtl);

        // Maintain a secondary index so we can invalidate all sessions for a user.
        var userSetKey = UserSessionsPrefix + data.UserId;
        await db.SetAddAsync(userSetKey, sessionId);
        await db.KeyExpireAsync(userSetKey, UserSessionsSetTtl);

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

    /// <summary>
    /// Deletes all active sessions for the specified user by consulting the
    /// secondary index set <c>user-sessions:{userId}</c>.
    /// Stale entries in the set (sessions already expired) are harmless — DEL on a
    /// missing key is a no-op in Redis.
    /// </summary>
    public async Task InvalidateAllSessionsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (_multiplexer is null) return; // No Redis; nothing to invalidate.

        var db = _multiplexer.GetDatabase();
        var userSetKey = UserSessionsPrefix + userId;

        var sessionIds = await db.SetMembersAsync(userSetKey);

        if (sessionIds.Length > 0)
        {
            var sessionKeys = sessionIds
                .Select(s => (RedisKey)(KeyPrefix + s))
                .ToArray();
            await db.KeyDeleteAsync(sessionKeys);
        }

        await db.KeyDeleteAsync(userSetKey);
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
