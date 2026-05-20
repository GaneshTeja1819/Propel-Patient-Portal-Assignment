namespace UPACIP.Application.Commands.Appointments;

/// <summary>
/// CQRS command for rescheduling an existing appointment.
/// </summary>
public record RescheduleAppointmentCommand(
    Guid PatientId,
    Guid AppointmentId,
    Guid NewSlotId);
