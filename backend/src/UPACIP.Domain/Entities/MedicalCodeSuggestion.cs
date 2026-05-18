namespace UPACIP.Domain.Entities;

public class MedicalCodeSuggestion : BaseEntity
{
    public Guid ClinicalDataId { get; set; }
    public Guid PatientId { get; set; }
    public string CodeSystem { get; set; } = string.Empty;   // ICD10 | CPT | SNOMED
    public string SuggestedCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset SuggestedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ExtractedClinicalData ClinicalData { get; set; } = null!;
    public User Patient { get; set; } = null!;
    public VerifiedMedicalCode? VerifiedCode { get; set; }
}
