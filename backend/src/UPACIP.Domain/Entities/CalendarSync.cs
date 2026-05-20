namespace UPACIP.Domain.Entities;

public class CalendarSync : BaseEntity
{
    public Guid UserId { get; set; }
    /// <summary>FK to the appointment this sync record belongs to (US_021, AC-001).</summary>
    public Guid? AppointmentId { get; set; }
    public string Provider { get; set; } = string.Empty;            // Google | Outlook
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    /// <summary>Sync status: Synced | Failed | Deleted | Pending (US_021, AC-001 – AC-004).</summary>
    public string SyncStatus { get; set; } = "Pending";
    /// <summary>Calendar event ID returned by the provider API for update/delete calls.</summary>
    public string? CalendarEventId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
}
