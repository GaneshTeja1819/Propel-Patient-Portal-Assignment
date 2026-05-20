namespace UPACIP.Application.Commands.Documents;

/// <summary>
/// Command for <c>POST /api/v1/documents/upload</c> (US_025, AC-001).
///
/// Carries the caller identity, document metadata, and the raw file bytes.
/// The <c>PatientId</c> is always extracted from the authenticated JWT <c>sub</c>
/// claim — never from the request body — to prevent IDOR attacks (OWASP A01).
/// </summary>
/// <param name="PatientId">Patient's user ID from the JWT sub claim.</param>
/// <param name="DocumentType">Document category (e.g. "Historical", "Lab").</param>
/// <param name="FileName">Original file name for storage path construction.</param>
/// <param name="MimeType">MIME type declared by the client (validated server-side).</param>
/// <param name="FileBytes">Raw PDF bytes; SHA-256 hashed for duplicate detection.</param>
public sealed record UploadDocumentCommand(
    Guid PatientId,
    string DocumentType,
    string FileName,
    string MimeType,
    byte[] FileBytes);

/// <summary>Result returned by <see cref="Handlers.Documents.UploadDocumentHandler"/>.</summary>
/// <param name="ClinicalDocumentId">Newly created document ID; returned as HTTP 201 body.</param>
/// <param name="ExtractionStatus">Always "Pending" at upload time (AC-001).</param>
public sealed record UploadDocumentResult(
    Guid ClinicalDocumentId,
    string ExtractionStatus);
