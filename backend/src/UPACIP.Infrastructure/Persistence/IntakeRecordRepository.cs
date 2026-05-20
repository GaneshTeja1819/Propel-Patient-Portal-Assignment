using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IIntakeRecordRepository"/> (US_018, AC-004).
/// </summary>
internal sealed class IntakeRecordRepository : IIntakeRecordRepository
{
    private readonly AppDbContext _context;

    public IntakeRecordRepository(AppDbContext context) => _context = context;

    /// <inheritdoc />
    public Task<IntakeRecord?> FindByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.IntakeRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, ct);

    /// <inheritdoc />
    public async Task AddAsync(IntakeRecord record, CancellationToken ct = default)
        => await _context.IntakeRecords.AddAsync(record, ct);

    /// <inheritdoc />
    public Task<Guid?> GetAppointmentPatientIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => (Guid?)a.PatientId)
            .FirstOrDefaultAsync(ct);
}
