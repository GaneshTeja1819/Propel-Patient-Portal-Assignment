namespace UPACIP.Application.Interfaces;

/// <summary>
/// Service contract for querying appointment slots.
/// Abstracts database access from the Application layer.
/// </summary>
public interface ISlotQueryService
{
    /// <summary>
    /// Retrieves available slots for the specified date, ordered by start time.
    /// </summary>
    /// <param name="date">The date for which to fetch slots.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available slots for the specified date.</returns>
    Task<IReadOnlyList<SlotQueryDto>> GetAvailableSlotsAsync(DateTime date, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for slot queries.
/// </summary>
public class SlotQueryDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsAvailable { get; set; }
}
