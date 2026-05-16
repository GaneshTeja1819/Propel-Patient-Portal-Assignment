using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Caching;

/// <summary>
/// Redis-backed slot availability cache enforcing the 5-second TTL mandated by
/// TR-005 / NFR-002.
///
/// All public methods catch <see cref="RedisException"/> and log a warning
/// rather than propagating — callers fall through to database reads on
/// Redis unavailability (AC-002 edge case).
/// </summary>
internal sealed class SlotCacheService : ISlotCacheService
{
    private const int SlotTtlSeconds = 5;   // TR-005, NFR-002
    private const string KeyPrefix = "slot:";

    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly ILogger<SlotCacheService> _logger;

    public SlotCacheService(IConnectionMultiplexer? multiplexer, ILogger<SlotCacheService> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    public async Task<string?> GetSlotAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (_multiplexer is null) return null;
        try
        {
            var value = await _multiplexer.GetDatabase().StringGetAsync(KeyPrefix + slotId);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis unavailable — falling back to database reads for slot {SlotId}.", slotId);
            return null;
        }
    }

    public async Task SetSlotAsync(string slotId, string value, CancellationToken cancellationToken = default)
    {
        if (_multiplexer is null) return;
        try
        {
            await _multiplexer.GetDatabase()
                .StringSetAsync(KeyPrefix + slotId, value, TimeSpan.FromSeconds(SlotTtlSeconds));
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis write failed for slot {SlotId} — availability not cached.", slotId);
        }
    }

    public async Task InvalidateSlotAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (_multiplexer is null) return;
        try
        {
            await _multiplexer.GetDatabase().KeyDeleteAsync(KeyPrefix + slotId);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis delete failed for slot {SlotId}.", slotId);
        }
    }
}
