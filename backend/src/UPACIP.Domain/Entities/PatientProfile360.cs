namespace UPACIP.Domain.Entities;

public class PatientProfile360 : BaseEntity
{
    public Guid PatientId { get; set; }
    public string EncryptedSummaryJson { get; set; } = string.Empty;  // AES-encrypted 360° summary (PHI)
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int ConflictCount { get; set; }

    /// <summary>
    /// De-duplication pipeline state: Pending → Processing → Completed | Failed.
    /// Tracks idempotency for <c>DeduplicationJob</c> (US_027, edge case: concurrent runs).
    /// </summary>
    public string DeduplicationStatus { get; set; } = "Pending";

    // Navigation
    public User Patient { get; set; } = null!;
}
