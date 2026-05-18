namespace UPACIP.Domain.Entities;

public class DataConflict : BaseEntity
{
    public Guid PatientId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string SourceValue { get; set; } = string.Empty;
    public string TargetValue { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Patient { get; set; } = null!;
}
