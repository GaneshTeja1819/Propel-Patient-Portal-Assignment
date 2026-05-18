namespace UPACIP.Domain.Entities;

public class CalendarSync : BaseEntity
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;            // Google | Outlook | Apple
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
}
