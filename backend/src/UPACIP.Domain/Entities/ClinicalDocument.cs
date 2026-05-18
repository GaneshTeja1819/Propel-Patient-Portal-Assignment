namespace UPACIP.Domain.Entities;

public class ClinicalDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string StoragePath { get; set; } = string.Empty;   // Cloud storage path placeholder
    public string DocumentType { get; set; } = string.Empty;  // SOAP_Note | LabResult | Prescription | Referral
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsProcessed { get; set; }

    // Navigation
    public User Patient { get; set; } = null!;
    public User? Provider { get; set; }
    public Appointment? Appointment { get; set; }
    public ICollection<ExtractedClinicalData> ExtractedData { get; set; } = [];
}
