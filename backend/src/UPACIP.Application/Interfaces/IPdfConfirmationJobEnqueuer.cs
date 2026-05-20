namespace UPACIP.Application.Interfaces;

/// <summary>
/// Abstracts enqueuing the PDF appointment confirmation background job (US_015).
/// Infrastructure wires this to Hangfire so Application has no Hangfire dependency.
/// </summary>
public interface IPdfConfirmationJobEnqueuer
{
    /// <summary>Enqueues a fire-and-forget job to generate the PDF confirmation for the given appointment.</summary>
    void Enqueue(Guid appointmentId, bool isReschedule = false);
}
