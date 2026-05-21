namespace UPACIP.Application.Commands.Appointments;

/// <summary>Removes a queue entry with a mandatory reason. Soft-delete via status update.</summary>
public sealed record RemoveQueueEntryCommand(Guid AppointmentId, string Reason, Guid StaffActorId);
