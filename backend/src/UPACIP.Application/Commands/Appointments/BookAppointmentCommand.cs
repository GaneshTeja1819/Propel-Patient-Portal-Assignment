namespace UPACIP.Application.Commands.Appointments;

/// <summary>
/// CQRS command for booking an appointment slot (AC-001, AC-003, AC-004).
/// </summary>
/// <param name="PatientId">ID of the authenticated patient making the booking.</param>
/// <param name="SlotId">Target appointment slot to book.</param>
/// <param name="PreferredSlotId">Optional preferred slot for waitlist registration.</param>
/// <param name="InsuranceProvider">Optional insurance provider name for soft validation (AC-003).</param>
/// <param name="InsuranceId">Optional insurance member ID for soft validation (AC-003).</param>
public record BookAppointmentCommand(
    Guid PatientId,
    Guid SlotId,
    Guid? PreferredSlotId,
    string? InsuranceProvider,
    string? InsuranceId);
