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

    // Navigation
    public MedicalCodeSuggestion Suggestion { get; set; } = null!;
    public User VerifiedBy { get; set; } = null!;
}
