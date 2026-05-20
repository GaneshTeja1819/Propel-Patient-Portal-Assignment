using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Persistence abstraction for appointment cancellation and reschedule workflows.
/// </summary>
public interface IAppointmentManagementStore
{
    Task<Appointment?> GetAppointmentWithSlotAsync(
        Guid appointmentId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<AppointmentSlot?> GetSlotByIdAsync(Guid slotId, CancellationToken cancellationToken = default);
}
