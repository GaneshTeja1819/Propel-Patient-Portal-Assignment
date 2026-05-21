namespace UPACIP.Application.Commands.Appointments;

/// <summary>Marks a queue entry as "Arrived" and stamps arrival time.</summary>
public sealed record ArriveQueueEntryCommand(Guid AppointmentId, Guid StaffActorId);
