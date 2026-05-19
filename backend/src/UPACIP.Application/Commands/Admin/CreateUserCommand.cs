namespace UPACIP.Application.Commands.Admin;

/// <summary>Command to create a new user account as an admin (AC-001).</summary>
/// <param name="ActorId">ID of the admin performing the creation.</param>
/// <param name="Email">New user's e-mail address.</param>
/// <param name="Password">Plaintext password; hashed before persistence.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Role">Role assigned at creation: Patient, Staff, or Admin.</param>
public sealed record CreateUserCommand(
    Guid ActorId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role);
