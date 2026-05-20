namespace UPACIP.Domain.Entities;

public class WaitlistEntry : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PreferredSlotId { get; set; }
    public Guid? ProviderId { get; set; }
    public DateTimeOffset RequestedDate { get; set; }
    public string? PreferredTimeRange { get; set; }
    public string Status { get; set; } = string.Empty;  // Pending | Notified | Booked | Expired
    public DateTimeOffset? NotifiedAt { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Patient { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;
    public AppointmentSlot PreferredSlot { get; set; } = null!;
    public User? Provider { get; set; }
}
