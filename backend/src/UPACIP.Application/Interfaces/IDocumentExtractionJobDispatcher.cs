namespace UPACIP.Application.Interfaces;

/// <summary>
/// Dispatches the AI clinical data extraction background job for a given document.
/// Decouples the Application layer from the Hangfire/Infrastructure assemblies (Clean Architecture).
/// Infrastructure provides the concrete <c>HangfireDocumentExtractionJobDispatcher</c>.
/// </summary>
public interface IDocumentExtractionJobDispatcher
{
    /// <summary>Enqueues the extraction job for <paramref name="clinicalDocumentId"/>.</summary>
    void Dispatch(Guid clinicalDocumentId);
}
