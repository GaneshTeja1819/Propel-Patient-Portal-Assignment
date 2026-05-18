namespace UPACIP.Domain.Entities;

public class AppointmentSlot : BaseEntity
{
    public Guid ProviderId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int DurationMinutes { get; set; }
    public string? RecurrencePattern { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Provider { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
}
