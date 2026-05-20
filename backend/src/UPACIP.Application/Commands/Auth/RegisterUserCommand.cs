namespace UPACIP.Application.Commands.Auth;

/// <summary>
/// Registration command payload for patient self-service sign-up.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName);
