using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Data-access abstraction for Staff queue management operations.
/// Covers reading today's appointments and fetching single appointments
/// for mutation (arrive, reorder, remove).
/// </summary>
public interface IQueueStore
{
    /// <summary>
    /// Returns all appointments whose slot falls on today's UTC date,
    /// ordered by <c>displayOrder</c> (nulls last), then by slot start time.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetTodaysAppointmentsAsync(
        DateOnly utcToday,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single appointment with its slot eagerly included.
    /// Returns <c>null</c> when not found.
    /// </summary>
    Task<Appointment?> GetAppointmentWithSlotAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any other appointment in today's queue has a slot
    /// whose <c>startTime</c> falls within ±<paramref name="windowMinutes"/>
    /// of <paramref name="referenceTime"/> and has a <c>displayOrder</c> value
    /// that collides with <paramref name="targetOrder"/>.
    /// </summary>
    Task<bool> HasSlotConflictAsync(
        Guid excludeAppointmentId,
        DateTimeOffset referenceTime,
        int targetOrder,
        int windowMinutes,
        CancellationToken cancellationToken = default);
}
