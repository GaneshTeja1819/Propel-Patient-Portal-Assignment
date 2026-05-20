namespace UPACIP.Application.Interfaces;

/// <summary>
/// Abstraction for enqueuing waitlist notifications after slot release.
/// </summary>
public interface IWaitlistNotificationJobEnqueuer
{
    void Enqueue(Guid slotId);
}
