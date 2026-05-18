namespace UPACIP.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid SlotId { get; set; }
    public string Status { get; set; } = string.Empty;   // Scheduled | Completed | Cancelled | NoShow
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Patient { get; set; } = null!;
    public User Provider { get; set; } = null!;
    public AppointmentSlot Slot { get; set; } = null!;
    public ICollection<IntakeRecord> IntakeRecords { get; set; } = [];
    public ICollection<ClinicalDocument> ClinicalDocuments { get; set; } = [];
}
