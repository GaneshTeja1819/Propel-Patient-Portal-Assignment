namespace UPACIP.Domain.Entities;

/// <summary>
/// A clinical data conflict detected during the deduplication pass (US_027).
/// Status is an open-string enum: "Open" | "Resolved" | "ReviewedUnresolved".
/// Severity is "High" | "Medium".
/// ConflictingValues stores a JSON array of competing source values.
/// RowVersion (PostgreSQL xmin) guards against concurrent resolution (AC-002).
/// </summary>
public class DataConflict : BaseEntity
{
    public Guid PatientId { get; set; }
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Open | Resolved | ReviewedUnresolved</summary>
    public string Status { get; set; } = "Open";

    /// <summary>High | Medium</summary>
    public string Severity { get; set; } = "Medium";

    /// <summary>JSON array: [{value, sourceLabel, sourceDocumentId, isAiExtracted}]</summary>
    public string ConflictingValues { get; set; } = "[]";

    /// <summary>Authoritative value chosen at resolution time.</summary>
    public string? CanonicalValue { get; set; }

    /// <summary>Staff user ID who resolved or reviewed this conflict.</summary>
    public Guid? ResolvedById { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User Patient { get; set; } = null!;
}
