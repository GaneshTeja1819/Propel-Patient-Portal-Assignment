namespace UPACIP.Application.Queries.Slots;

/// <summary>
/// Response DTO for available slots query.
/// 
/// Includes a <see cref="Cached"/> flag to indicate whether data came from Redis
/// or PostgreSQL fallback.
/// </summary>
public class GetSlotsResult
{
    public List<SlotDto> Slots { get; set; } = [];
    
    /// <summary>
    /// True if result came from Redis cache; false if from PostgreSQL fallback.
    /// </summary>
    public bool Cached { get; set; }
}

/// <summary>
/// Slot data transfer object.
/// </summary>
public class SlotDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// Slot state: "Available" or "Unavailable".
    /// </summary>
    public string Status { get; set; } = "Available";
}
