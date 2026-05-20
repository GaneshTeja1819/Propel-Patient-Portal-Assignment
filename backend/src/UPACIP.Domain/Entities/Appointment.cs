namespace UPACIP.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid SlotId { get; set; }
    public string Status { get; set; } = string.Empty;   // Booked | Scheduled | Completed | Cancelled | NoShow
    public string? Notes { get; set; }
    public int NoShowRiskScore { get; set; }
    public string InsuranceValidationStatus { get; set; } = "NotProvided";  // Validated | NotRecognised | NotProvided
    public string? InsuranceProvider { get; set; }
    public string? InsuranceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// JSON-serialised array of Hangfire job IDs for scheduled reminder jobs.
    /// Stored so reminders can be cancelled on reschedule (US_020, AC-001).
    /// </summary>
    public string? ReminderJobIds { get; set; }

    // Navigation
    public User Patient { get; set; } = null!;
    public User Provider { get; set; } = null!;
    public AppointmentSlot Slot { get; set; } = null!;
    public ICollection<IntakeRecord> IntakeRecords { get; set; } = [];
    public ICollection<ClinicalDocument> ClinicalDocuments { get; set; } = [];
}
