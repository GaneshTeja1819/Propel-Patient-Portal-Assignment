namespace UPACIP.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;    // AppointmentReminder | WaitlistUpdate | SystemAlert
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ScheduledFor { get; set; }   // Hangfire-scheduled delivery time
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Recipient { get; set; } = null!;
}
