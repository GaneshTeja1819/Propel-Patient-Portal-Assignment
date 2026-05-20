namespace UPACIP.Application.Commands.Appointments;

/// <summary>
/// CQRS command for cancelling an existing appointment.
/// </summary>
public record CancelAppointmentCommand(
    Guid PatientId,
    Guid AppointmentId);
