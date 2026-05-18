namespace UPACIP.Application.Interfaces;

/// <summary>
/// Extracts plain text from a PDF document for downstream AI processing.
/// Implementations must be safe to call from background jobs and
/// must never throw — a corrupted or unreadable PDF returns <see cref="string.Empty"/>.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts all text from <paramref name="pdfBytes"/> and returns it
    /// as a single concatenated string.
    /// </summary>
    /// <param name="pdfBytes">Raw PDF file content.</param>
    /// <param name="documentId">
    /// Identifier of the source document — included in error log entries
    /// so corrupted-PDF incidents can be correlated to the originating upload.
    /// </param>
    /// <returns>
    /// Concatenated page text on success; <see cref="string.Empty"/> when the
    /// PDF is corrupted or otherwise unreadable (error is logged by the implementation).
    /// </returns>
    string ExtractText(byte[] pdfBytes, Guid documentId);
}
