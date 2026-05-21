namespace UPACIP.Application.Queries.Queue;

/// <summary>
/// Query to retrieve all appointments for today's queue in display order.
/// </summary>
public sealed record GetTodaysQueueQuery(DateOnly UtcToday);
