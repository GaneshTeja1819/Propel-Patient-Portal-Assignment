namespace UPACIP.Application.Queries.Slots;

/// <summary>
/// Query to retrieve available appointment slots for a specific date.
/// 
/// Acceptance Criteria (AC-001):
/// - Slot data served from Redis cache within 100ms on cache hit
/// - Falls through to PostgreSQL if Redis unavailable
/// 
/// AC-002:
/// - Slot state changes reflected in cache within one 5-second TTL cycle
/// </summary>
public record GetSlotsQuery(DateTime Date);
