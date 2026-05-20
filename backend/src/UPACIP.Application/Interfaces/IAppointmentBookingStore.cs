using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Persistence abstraction for appointment booking operations.
/// Infrastructure implements this; Application orchestrates via it.
/// EF Core concurrency exceptions are handled here and returned as false (AC-004).
/// </summary>
public interface IAppointmentBookingStore
{
    /// <summary>
    /// Atomically marks the slot as unavailable (IsAvailable = false).
    /// Returns <see langword="true"/> when the slot was successfully acquired;
    /// returns <see langword="false"/> if the slot was already unavailable or
    /// a concurrent request acquired it first (DbUpdateConcurrencyException → false).
    /// </summary>
    Task<bool> TryAcquireSlotAsync(Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>Returns the ProviderId for the given slot (for Appointment creation).</summary>
    Task<Guid?> GetSlotProviderIdAsync(Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>Returns the StartTime for the given slot (for risk scoring lead time).</summary>
    Task<DateTimeOffset?> GetSlotStartTimeAsync(Guid slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates insurance: returns true when an active InsuranceRecord with matching
    /// ProviderName exists and the supplied ID satisfies its InsuranceIdPattern regex.
    /// Returns false when no match is found; returns null when no record for provider exists.
    /// </summary>
    Task<bool?> ValidateInsuranceAsync(string providerName, string insuranceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of past appointments with Status = "NoShow" for the patient.</summary>
    Task<int> CountPatientNoShowsAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Persists the appointment entity (includes SaveChanges call).</summary>
    Task<Guid> SaveAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a waitlist entry for the same patient+preferred slot pair.
    /// Existing rows are updated with latest appointment and registration timestamp.
    /// </summary>
    Task UpsertWaitlistEntryAsync(
        Guid patientId,
        Guid appointmentId,
        Guid preferredSlotId,
        Guid? providerId,
        DateTimeOffset requestedDate,
        CancellationToken cancellationToken = default);
}
