namespace UPACIP.Domain.Entities;

public class IntakeRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string EncryptedFormData { get; set; } = string.Empty;  // AES-encrypted JSON (PHI)
    public string FormType { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsReviewed { get; set; }

    // Navigation
    public User Patient { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}
