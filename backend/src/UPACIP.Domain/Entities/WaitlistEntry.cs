namespace UPACIP.Domain.Entities;

public class WaitlistEntry : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? ProviderId { get; set; }
    public DateTimeOffset RequestedDate { get; set; }
    public string? PreferredTimeRange { get; set; }
    public string Status { get; set; } = string.Empty;  // Pending | Notified | Booked | Expired
    public DateTimeOffset? NotifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Patient { get; set; } = null!;
    public User? Provider { get; set; }
}
