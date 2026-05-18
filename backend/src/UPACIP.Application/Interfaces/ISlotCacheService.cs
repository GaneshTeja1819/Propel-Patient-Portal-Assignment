namespace UPACIP.Application.Interfaces;

/// <summary>
/// Slot availability cache abstraction (NFR-002, TR-005).
///
/// Infrastructure implements this using Upstash Redis with a 5-second TTL on
/// every write. On Redis unavailability, implementations MUST return
/// <see langword="null"/> rather than throw — callers fall through to
/// direct database reads (AC-002 edge case).
/// </summary>
public interface ISlotCacheService
{
    /// <summary>Returns the cached JSON payload for <paramref name="slotId"/>,
    /// or <see langword="null"/> if the key is absent or Redis is unavailable.</summary>
    Task<string?> GetSlotAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>Writes <paramref name="value"/> under <paramref name="slotId"/>
    /// with a mandatory 5-second TTL (NFR-002). Silently no-ops when Redis is
    /// unavailable.</summary>
    Task SetSlotAsync(string slotId, string value, CancellationToken cancellationToken = default);

    /// <summary>Removes <paramref name="slotId"/> from the cache immediately.
    /// Silently no-ops when Redis is unavailable.</summary>
    Task InvalidateSlotAsync(string slotId, CancellationToken cancellationToken = default);
}
