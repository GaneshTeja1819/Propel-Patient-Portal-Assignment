using UPACIP.Domain.Entities;

namespace UPACIP.Application.Interfaces;

/// <summary>
/// Narrow repository for <see cref="IntakeRecord"/> persistence operations
/// used by <c>ConfirmIntakeHandler</c> (US_018, AC-004).
///
/// Changes are staged in the EF Core change tracker; callers must commit via
/// <see cref="IUnitOfWork.SaveChangesAsync"/> to flush to the database.
/// </summary>
public interface IIntakeRecordRepository
{
    /// <summary>
    /// Returns the first <see cref="IntakeRecord"/> whose
    /// <see cref="IntakeRecord.AppointmentId"/> matches, or
    /// <see langword="null"/> when no record exists.
    /// Used for the idempotency check (AC-004 edge case).
    /// </summary>
    Task<IntakeRecord?> FindByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="IntakeRecord"/> for insertion on the next commit.</summary>
    Task AddAsync(IntakeRecord record, CancellationToken ct = default);

    /// <summary>
    /// Returns the <c>PatientId</c> foreign key on the appointment, or
    /// <see langword="null"/> if the appointment does not exist.
    /// Used for ownership validation (OWASP A01 / CR-001 fix).
    /// </summary>
    Task<Guid?> GetAppointmentPatientIdAsync(Guid appointmentId, CancellationToken ct = default);
}
