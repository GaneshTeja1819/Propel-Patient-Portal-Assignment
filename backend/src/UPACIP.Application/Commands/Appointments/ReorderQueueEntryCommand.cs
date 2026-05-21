namespace UPACIP.Application.Commands.Appointments;

/// <summary>Updates the display order of a queue entry. Always applied — conflict is advisory only.</summary>
public sealed record ReorderQueueEntryCommand(Guid AppointmentId, int NewIndex, Guid StaffActorId);
