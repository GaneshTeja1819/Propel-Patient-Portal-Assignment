namespace UPACIP.Infrastructure.Documents;

/// <summary>
/// Thrown by <see cref="SupabaseStorageService"/> when the Supabase Storage
/// backend returns HTTP 413 (Payload Too Large) or HTTP 507 (Insufficient
/// Storage), signalling that the configured bucket quota is exhausted.
/// The upload controller maps this to HTTP 507 (AC-001 edge case).
/// </summary>
public sealed class StorageLimitExceededException : Exception
{
    public StorageLimitExceededException()
        : base("Storage limit exceeded — the document could not be saved.") { }

    public StorageLimitExceededException(string message)
        : base(message) { }
}
