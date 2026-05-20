using System.Text.Json;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;
using UPACIP.Application.Queries.Slots;

namespace UPACIP.Application.Handlers.Slots;

/// <summary>
/// Retrieves available appointment slots for a date with Redis caching.
/// 
/// Implementation Plan (AC-001, AC-002):
/// 1. Construct cache key: slots:date:{date:yyyy-MM-dd}
/// 2. Try to read from Redis cache (5-second TTL)
/// 3. On cache miss/unavailability, query PostgreSQL for AppointmentSlot where date matches and IsAvailable = true
/// 4. Serialize slots to JSON and cache for 5 seconds
/// 5. Return result with cached=true/false flag
/// 
/// Edge Cases:
/// - Redis unavailable: gracefully fall through to database query (AC-002)
/// - No slots available: return empty list with cached flag
/// - Invalid date format: handled by query validation
/// </summary>
public sealed class GetSlotsHandler
{
    private const string CacheKeyPrefix = "slots:date:";
    
    private readonly ISlotQueryService _slotQueryService;
    private readonly ISlotCacheService _cacheService;
    private readonly ILogger<GetSlotsHandler> _logger;

    public GetSlotsHandler(
        ISlotQueryService slotQueryService,
        ISlotCacheService cacheService,
        ILogger<GetSlotsHandler> logger)
    {
        _slotQueryService = slotQueryService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GetSlotsResult> HandleAsync(
        GetSlotsQuery query,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{query.Date:yyyy-MM-dd}";
        
        // AC-001: Try Redis cache first
        var cachedJson = await _cacheService.GetSlotAsync(cacheKey, cancellationToken);
        if (cachedJson is not null)
        {
            try
            {
                var slots = JsonSerializer.Deserialize<List<SlotDto>>(cachedJson) ?? [];
                _logger.LogInformation("Returning {SlotCount} slots from cache for {Date}.", slots.Count, query.Date);
                return new GetSlotsResult { Slots = slots, Cached = true };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached slots for {Date}; falling back to database.", query.Date);
            }
        }

        // AC-002: Fall through to PostgreSQL on cache miss/error
        _logger.LogInformation("Cache miss for {Date}; querying PostgreSQL.", query.Date);
        
        var slotsFromDb = await _slotQueryService.GetAvailableSlotsAsync(query.Date, cancellationToken);

        var slotDtos = slotsFromDb
            .Select(s => new SlotDto
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                StartTime = s.StartTime.DateTime,
                EndTime = s.EndTime.DateTime,
                DurationMinutes = s.DurationMinutes,
                Status = s.IsAvailable ? "Available" : "Unavailable"
            })
            .ToList();

        // AC-001: Write to cache with 5-second TTL
        var json = JsonSerializer.Serialize(slotDtos);
        await _cacheService.SetSlotAsync(cacheKey, json, cancellationToken);

        _logger.LogInformation("Cached {SlotCount} slots for {Date} from PostgreSQL.", slotDtos.Count, query.Date);
        
        return new GetSlotsResult { Slots = slotDtos, Cached = false };
    }
}
