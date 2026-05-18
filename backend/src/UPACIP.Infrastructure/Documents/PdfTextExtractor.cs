using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Documents;

/// <summary>
/// Extracts text from PDF documents using the PdfPig library.
/// Designed for use in background AI-pipeline jobs — never throws;
/// corrupted or unreadable PDFs return <see cref="string.Empty"/>.
/// </summary>
internal sealed class PdfTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfTextExtractor> _logger;

    public PdfTextExtractor(ILogger<PdfTextExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string ExtractText(byte[] pdfBytes, Guid documentId)
    {
        try
        {
            using var document = PdfDocument.Open(pdfBytes);

            var builder = new System.Text.StringBuilder();
            foreach (var page in document.GetPages())
            {
                builder.Append(page.Text);
                builder.Append(' ');
            }

            return builder.ToString().Trim();
        }
        catch (Exception ex)
        {
            // AC-005: corrupted PDF → return empty string and log the document ID
            // with the error so the incident can be traced to the originating upload.
            _logger.LogError(ex,
                "Failed to extract text from PDF document {DocumentId}: {Message}",
                documentId, ex.Message);

            return string.Empty;
        }
    }
}
