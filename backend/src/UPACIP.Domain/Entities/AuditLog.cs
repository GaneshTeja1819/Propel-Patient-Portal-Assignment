namespace UPACIP.Domain.Entities;

/// <summary>
/// Immutable audit record. ActorId is stored as a plain Guid with NO foreign key
/// constraint because the acting user may be deleted after the event (HIPAA
/// audit trail must survive user deletion).
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? ActorId { get; set; }                        // No FK — actor may be deleted
    public string ActorEmail { get; set; } = string.Empty;   // Stored for forensics
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
