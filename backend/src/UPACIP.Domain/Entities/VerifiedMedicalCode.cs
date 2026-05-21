namespace UPACIP.Domain.Entities;

public class VerifiedMedicalCode : BaseEntity
{
    public Guid SuggestionId { get; set; }
    public Guid VerifiedById { get; set; }
    public string CodeSystem { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset VerifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
    public string Decision { get; set; } = string.Empty;              // AC-002/03/04: "Accepted" | "Modified" | "Rejected"
    public string? OriginalSuggestedCode { get; set; }                // AC-003: AI-suggested code before modification; null for Accepted/Rejected

    // Navigation
    public MedicalCodeSuggestion Suggestion { get; set; } = null!;
    public User VerifiedBy { get; set; } = null!;
}
