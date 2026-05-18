namespace UPACIP.Application.Exceptions;

/// <summary>
/// Thrown when a user account is temporarily locked due to failed attempts.
/// </summary>
public sealed class AccountLockedException : Exception
{
    public AccountLockedException(DateTimeOffset lockUntil)
        : base("Account temporarily locked")
    {
        LockUntil = lockUntil;
    }

    public DateTimeOffset LockUntil { get; }
}
