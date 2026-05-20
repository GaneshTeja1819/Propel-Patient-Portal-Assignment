namespace UPACIP.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;          // Patient | Provider | Admin | Staff
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Appointment> PatientAppointments { get; set; } = [];
    public ICollection<Appointment> ProviderAppointments { get; set; } = [];
    public ICollection<AppointmentSlot> AppointmentSlots { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<CalendarSync> CalendarSyncs { get; set; } = [];
    public PatientProfile360? PatientProfile { get; set; }

    /// <summary>
    /// Used to compute the <c>isNew</c> flag on DataConflict records.
    /// Null means the patient has never reviewed any conflicts.
    /// Updated to UtcNow after each Staff resolve or mark-reviewed action.
    /// </summary>
    public DateTimeOffset? LastConflictReviewedAt { get; set; }
}
