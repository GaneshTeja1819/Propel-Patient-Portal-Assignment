namespace UPACIP.Application.Commands.Auth;

/// <summary>
/// Login command payload for email/password authentication.
/// </summary>
public sealed record LoginUserCommand(string Email, string Password);

public sealed record LoginUserResult(string AccessToken, string SessionId, string Role);
