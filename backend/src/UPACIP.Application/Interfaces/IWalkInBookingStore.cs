using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Persistence abstraction for Staff walk-in booking workflows.
/// </summary>
public interface IWalkInBookingStore
{
    /// <summary>Returns the appointment slot, or <see langword="null"/> if not found.</summary>
    Task<AppointmentSlot?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new appointment for persistence (SaveChanges commits it).</summary>
    Task AddAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default);

    /// <summary>Returns the appointment, or <see langword="null"/> if not found.</summary>
    Task<Appointment?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Returns patients whose full name matches the query (case-insensitive).</summary>
    Task<IReadOnlyList<PatientSearchResult>> SearchPatientsAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>Lightweight patient projection returned by the type-ahead search endpoint.</summary>
public sealed record PatientSearchResult(Guid Id, string FullName, string Email);
