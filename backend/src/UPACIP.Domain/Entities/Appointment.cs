namespace UPACIP.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid? PatientId { get; set; }                           // Null for anonymous walk-in bookings
    public Guid ProviderId { get; set; }
    public Guid SlotId { get; set; }
    public Guid? CreatedByStaffId { get; set; }                    // Set when Staff creates the appointment
    public string? AnonymousPatientDetails { get; set; }           // JSON; populated when PatientId is null
    public string Status { get; set; } = string.Empty;             // Scheduled | Arrived | Completed | Cancelled | NoShow | RemovedFromQueue
    public string? Notes { get; set; }
    public int? DisplayOrder { get; set; }                         // Queue display order; null = not explicitly ordered
    public DateTimeOffset? ArrivedAt { get; set; }                 // Set when Staff marks patient as Arrived
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User? Patient { get; set; }                             // Nullable — absent for anonymous bookings
    public User Provider { get; set; } = null!;
    public AppointmentSlot Slot { get; set; } = null!;
    public ICollection<IntakeRecord> IntakeRecords { get; set; } = [];
    public ICollection<ClinicalDocument> ClinicalDocuments { get; set; } = [];
}
