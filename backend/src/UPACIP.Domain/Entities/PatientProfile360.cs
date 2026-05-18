namespace UPACIP.Domain.Entities;

public class PatientProfile360 : BaseEntity
{
    public Guid PatientId { get; set; }
    public string EncryptedSummaryJson { get; set; } = string.Empty;  // AES-encrypted 360° summary (PHI)
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int ConflictCount { get; set; }

    // Navigation
    public User Patient { get; set; } = null!;
}
