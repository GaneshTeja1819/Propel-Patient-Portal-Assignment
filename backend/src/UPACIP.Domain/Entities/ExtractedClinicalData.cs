namespace UPACIP.Domain.Entities;

public class ExtractedClinicalData : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid PatientId { get; set; }
    public string EncryptedExtractedJson { get; set; } = string.Empty;  // AES-encrypted extraction (PHI)
    public string ExtractionModel { get; set; } = string.Empty;          // Model identifier (e.g. gemini-2.0-flash)
    public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;
    public double? ConfidenceScore { get; set; }

    // Navigation
    public ClinicalDocument Document { get; set; } = null!;
    public User Patient { get; set; } = null!;
    public ICollection<MedicalCodeSuggestion> CodeSuggestions { get; set; } = [];
}
