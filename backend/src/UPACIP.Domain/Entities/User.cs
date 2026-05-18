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
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Appointment> PatientAppointments { get; set; } = [];
    public ICollection<Appointment> ProviderAppointments { get; set; } = [];
    public ICollection<AppointmentSlot> AppointmentSlots { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<CalendarSync> CalendarSyncs { get; set; } = [];
    public PatientProfile360? PatientProfile { get; set; }
}
