using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

internal sealed class QueueStore : IQueueStore
{
    private readonly AppDbContext _context;

    public QueueStore(AppDbContext context) => _context = context;

    public Task<IReadOnlyList<Appointment>> GetTodaysAppointmentsAsync(
        DateOnly utcToday,
        CancellationToken cancellationToken = default)
    {
        // Convert DateOnly to DateTimeOffset range for comparison
        var dayStart = new DateTimeOffset(utcToday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd   = dayStart.AddDays(1);

        return _context.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Patient)
            .Where(a => a.Slot.StartTime >= dayStart && a.Slot.StartTime < dayEnd
                        && a.Status != "RemovedFromQueue")
            .OrderBy(a => a.DisplayOrder == null ? 1 : 0)
            .ThenBy(a => a.DisplayOrder)
            .ThenBy(a => a.Slot.StartTime)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Appointment>)t.Result, cancellationToken);
    }

    public Task<Appointment?> GetAppointmentWithSlotAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
        => _context.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

    public Task<bool> HasSlotConflictAsync(
        Guid excludeAppointmentId,
        DateTimeOffset referenceTime,
        int targetOrder,
        int windowMinutes,
        CancellationToken cancellationToken = default)
    {
        var windowStart = referenceTime.AddMinutes(-windowMinutes);
        var windowEnd   = referenceTime.AddMinutes(windowMinutes);

        return _context.Appointments
            .Include(a => a.Slot)
            .AnyAsync(a =>
                a.Id != excludeAppointmentId
                && a.DisplayOrder == targetOrder
                && a.Slot.StartTime >= windowStart
                && a.Slot.StartTime <= windowEnd
                && a.Status != "RemovedFromQueue",
                cancellationToken);
    }
}
