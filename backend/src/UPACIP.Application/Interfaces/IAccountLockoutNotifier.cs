namespace UPACIP.Application.Interfaces;

/// <summary>
/// Enqueues account lockout notifications for out-of-band delivery.
/// </summary>
public interface IAccountLockoutNotifier
{
    void Enqueue(string email, DateTimeOffset lockUntilUtc);
}
