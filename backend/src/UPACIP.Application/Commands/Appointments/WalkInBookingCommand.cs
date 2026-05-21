namespace UPACIP.Application.Commands.Appointments;

/// <summary>
/// Command for creating a walk-in appointment initiated by Staff.
/// Exactly one of <see cref="PatientId"/> (existing patient) or
/// <see cref="AnonName"/> (anonymous) must be provided.
/// </summary>
public sealed record WalkInBookingCommand(
    Guid? PatientId,
    string? AnonName,
    DateOnly? AnonDob,
    string? AnonPhone,
    Guid SlotId,
    Guid StaffActorId);
