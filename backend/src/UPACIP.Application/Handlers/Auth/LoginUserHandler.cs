using Microsoft.Extensions.Configuration;
using UPACIP.Application.Commands.Auth;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Auth;

/// <summary>
/// Handles authentication with lockout and audit behavior.
/// </summary>
public sealed class LoginUserHandler
{
    private const string InvalidCredentialsMessage = "Invalid email or password";
    private static readonly string DummyHash = BCrypt.Net.BCrypt.EnhancedHashPassword("dummy-password", 6);

    private readonly ILoginUserStore _loginUserStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAccountLockoutNotifier _accountLockoutNotifier;
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockDuration;

    public LoginUserHandler(
        ILoginUserStore loginUserStore,
        IUnitOfWork unitOfWork,
        IAuthService authService,
        IAuditLogService auditLogService,
        IAccountLockoutNotifier accountLockoutNotifier,
        IConfiguration configuration)
    {
        _loginUserStore = loginUserStore;
        _unitOfWork = unitOfWork;
        _authService = authService;
        _auditLogService = auditLogService;
        _accountLockoutNotifier = accountLockoutNotifier;
        _maxFailedAttempts = configuration.GetValue<int?>("Auth:Lockout:MaxFailedAttempts") ?? 5;
        var lockDurationMinutes = configuration.GetValue<int?>("Auth:Lockout:DurationMinutes") ?? 15;
        _lockDuration = TimeSpan.FromMinutes(lockDurationMinutes);
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await _loginUserStore.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            VerifyAgainstDummyHash(command.Password);
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        if (user.LockUntil.HasValue && user.LockUntil.Value > DateTimeOffset.UtcNow)
            throw new AccountLockedException(user.LockUntil.Value);

        var passwordValid = BCrypt.Net.BCrypt.EnhancedVerify(command.Password, user.PasswordHash);

        if (!passwordValid)
        {
            user.FailedLoginCount += 1;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            if (user.FailedLoginCount >= _maxFailedAttempts)
            {
                user.LockUntil = DateTimeOffset.UtcNow.Add(_lockDuration);
                _accountLockoutNotifier.Enqueue(user.Email, user.LockUntil.Value);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);

        user.FailedLoginCount = 0;
        user.LockUntil = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authResult = await _authService.IssueSessionAsync(user.Id, user.Role, user.Email, cancellationToken);

        await _auditLogService.LogAsync(
            actorId: user.Id,
            actorRole: user.Role,
            actionType: "USER_LOGIN",
            targetEntity: "User",
            targetId: user.Id,
            cancellationToken: cancellationToken);

        return new LoginUserResult(authResult.AccessToken, authResult.SessionId, user.Role);
    }

    private static void VerifyAgainstDummyHash(string password)
    {
        BCrypt.Net.BCrypt.EnhancedVerify(password, DummyHash);
    }
}
