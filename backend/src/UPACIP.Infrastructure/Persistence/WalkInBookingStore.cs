using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

internal sealed class WalkInBookingStore : IWalkInBookingStore
{
    private readonly AppDbContext _context;

    public WalkInBookingStore(AppDbContext context) => _context = context;

    public Task<AppointmentSlot?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
        => _context.AppointmentSlots
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);

    public Task AddAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => _context.Appointments.AddAsync(appointment, cancellationToken).AsTask();

    public Task<Appointment?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        => _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

    public async Task<IReadOnlyList<PatientSearchResult>> SearchPatientsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Case-insensitive full-name search; limited to 20 results for type-ahead performance.
        var q = query.Trim();
        return await _context.Users
            .Where(u => u.Role == "Patient"
                && EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{q}%"))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Take(20)
            .Select(u => new PatientSearchResult(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.Email))
            .ToListAsync(cancellationToken);
    }
}
