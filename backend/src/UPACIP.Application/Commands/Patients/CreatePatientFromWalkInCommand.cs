namespace UPACIP.Application.Commands.Patients;

/// <summary>
/// Command for creating a Patient <c>User</c> record from an existing anonymous
/// walk-in <see cref="UPACIP.Domain.Entities.Appointment"/> and linking the two.
/// </summary>
public sealed record CreatePatientFromWalkInCommand(
    Guid AppointmentId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid StaffActorId);
