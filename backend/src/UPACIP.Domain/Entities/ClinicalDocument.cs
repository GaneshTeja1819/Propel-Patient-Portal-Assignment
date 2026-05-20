namespace UPACIP.Domain.Entities;

public class ClinicalDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string StoragePath { get; set; } = string.Empty;   // Encrypted pointer to Supabase Storage (AC-005, DR-005)
    public string DocumentType { get; set; } = string.Empty;  // Historical | Post-visit | Lab | Imaging | Insurance
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsProcessed { get; set; }
    /// <summary>
    /// SHA-256 hex digest of the raw file bytes. Used for duplicate detection
    /// on a per-patient basis (edge case: same patient re-uploads same file).
    /// Not considered PHI — stored unencrypted; indexed for fast lookup.
    /// </summary>
    public string FileHash { get; set; } = string.Empty;
    /// <summary>
    /// AI extraction pipeline state machine: Pending → Processing → Completed | Failed.
    /// Set to "Pending" on document creation (AC-001); transitions managed by
    /// <c>ClinicalDataExtractionJob</c> (US_026, AC-004).
    /// </summary>
    public string ExtractionStatus { get; set; } = "Pending";

    /// <summary>
    /// Human-readable reason stored when <see cref="ExtractionStatus"/> is "Failed"
    /// (US_026, AC-004, AIR-007). Cleared to <c>null</c> when a retry is requested.
    /// Not considered PHI — stored unencrypted; safe to surface via the
    /// <c>GET /extraction-status</c> endpoint (UXR-603).
    /// </summary>
    public string? ExtractionFailureNote { get; set; }

    // Navigation
    public User Patient { get; set; } = null!;
    public User? Provider { get; set; }
    public Appointment? Appointment { get; set; }
    public ICollection<ExtractedClinicalData> ExtractedData { get; set; } = [];
}
