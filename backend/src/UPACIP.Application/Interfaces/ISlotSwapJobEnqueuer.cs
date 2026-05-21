namespace UPACIP.Application.Interfaces;

/// <summary>
/// Abstraction for enqueuing slot-swap evaluation after a slot is released.
/// </summary>
public interface ISlotSwapJobEnqueuer
{
    void Enqueue(Guid releasedSlotId);
}
