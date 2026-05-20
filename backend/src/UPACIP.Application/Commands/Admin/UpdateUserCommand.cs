namespace UPACIP.Application.Commands.Admin;

/// <summary>Command to update an existing user's profile fields (AC-001).</summary>
/// <param name="ActorId">ID of the admin performing the change.</param>
/// <param name="TargetUserId">ID of the user being updated.</param>
/// <param name="FirstName">New first name, or <see langword="null"/> to leave unchanged.</param>
/// <param name="LastName">New last name, or <see langword="null"/> to leave unchanged.</param>
/// <param name="Email">New e-mail, or <see langword="null"/> to leave unchanged.</param>
/// <param name="PhoneNumber">New phone number; empty string clears the field; <see langword="null"/> leaves unchanged.</param>
public sealed record UpdateUserCommand(
    Guid ActorId,
    Guid TargetUserId,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber);
