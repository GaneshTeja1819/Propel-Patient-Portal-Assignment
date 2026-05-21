namespace UPACIP.Application.Interfaces;

/// <summary>
/// Abstracts cloud object storage for clinical PDF documents (AC-001, AC-005).
/// Implementations must not persist binary content in the relational database (DR-005).
/// Credentials must originate exclusively from environment variables (OWASP A02).
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Uploads <paramref name="bytes"/> to cloud storage and returns the
    /// storage path (e.g. <c>patients/{patientId}/{fileName}</c>).
    /// The caller is responsible for encrypting the returned path before
    /// persisting it to the database.
    /// </summary>
    /// <exception cref="StorageLimitExceededException">
    /// Thrown when the storage provider signals that the storage quota has
    /// been exhausted (HTTP 413 or 507 from the storage backend).
    /// </exception>
    Task<string> UploadAsync(
        Guid patientId,
        string fileName,
        byte[] bytes,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads the document at <paramref name="storagePath"/> and returns its raw bytes.
    /// <paramref name="storagePath"/> is the value returned by <see cref="UploadAsync"/>
    /// (e.g. <c>patients/{patientId}/{fileName}</c>); the caller must ensure it has
    /// already been decrypted before passing it here.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the storage provider returns a non-success HTTP status code.
    /// </exception>
    Task<byte[]> DownloadAsync(string storagePath, CancellationToken ct = default);
}
