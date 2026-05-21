using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core store used by cancel/reschedule appointment handlers.
/// </summary>
internal sealed class AppointmentManagementStore : IAppointmentManagementStore
{
    private readonly AppDbContext _dbContext;

    public AppointmentManagementStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Appointment?> GetAppointmentWithSlotAsync(
        Guid appointmentId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(
                a => a.Id == appointmentId && a.PatientId == patientId,
                cancellationToken);
    }

    public async Task<AppointmentSlot?> GetSlotByIdAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppointmentSlots
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
    }
}
