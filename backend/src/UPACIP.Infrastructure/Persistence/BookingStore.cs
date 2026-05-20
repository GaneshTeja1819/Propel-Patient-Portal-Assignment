using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAppointmentBookingStore"/>.
///
/// TryAcquireSlotAsync handles DbUpdateConcurrencyException internally
/// so the Application layer has no EF Core dependency (AC-004).
/// </summary>
internal sealed class BookingStore : IAppointmentBookingStore
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BookingStore> _logger;

    public BookingStore(AppDbContext dbContext, ILogger<BookingStore> logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        var slot = await _dbContext.AppointmentSlots
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);

        if (slot is null)
        {
            _logger.LogWarning("TryAcquireSlot: slot {SlotId} not found.", slotId);
            return false;
        }

        if (!slot.IsAvailable)
            return false;

        slot.IsAvailable = false;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogInformation(
                ex,
                "Concurrency conflict on slot {SlotId}; returning false.",
                slotId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Guid?> GetSlotProviderIdAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppointmentSlots
            .Where(s => s.Id == slotId)
            .Select(s => (Guid?)s.ProviderId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetSlotStartTimeAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppointmentSlots
            .Where(s => s.Id == slotId)
            .Select(s => (DateTimeOffset?)s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool?> ValidateInsuranceAsync(
        string providerName,
        string insuranceId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.InsuranceRecords
            .AsNoTracking()
            .Where(r => r.IsActive && r.ProviderName == providerName)
            .Select(r => r.InsuranceIdPattern)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
            return null;   // Provider not found

        try
        {
            return Regex.IsMatch(insuranceId, record, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            _logger.LogWarning("Insurance regex match timed out for provider {ProviderName}.", providerName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> CountPatientNoShowsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .AsNoTracking()
            .CountAsync(a => a.PatientId == patientId && a.Status == "NoShow", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> SaveAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return appointment.Id;
    }

    /// <inheritdoc />
    public async Task UpsertWaitlistEntryAsync(
        Guid patientId,
        Guid appointmentId,
        Guid preferredSlotId,
        Guid? providerId,
        DateTimeOffset requestedDate,
        CancellationToken cancellationToken = default)
    {
        var registeredAt = DateTimeOffset.UtcNow;

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO waitlist_entries
                (""PatientId"", ""AppointmentId"", ""PreferredSlotId"", ""ProviderId"", ""RequestedDate"", ""Status"", ""RegisteredAt"", ""CreatedAt"")
            VALUES
                ({patientId}, {appointmentId}, {preferredSlotId}, {providerId}, {requestedDate}, {"Pending"}, {registeredAt}, {registeredAt})
            ON CONFLICT (""PatientId"", ""PreferredSlotId"") DO UPDATE
            SET
                ""AppointmentId"" = EXCLUDED.""AppointmentId"",
                ""ProviderId"" = EXCLUDED.""ProviderId"",
                ""RequestedDate"" = EXCLUDED.""RequestedDate"",
                ""Status"" = EXCLUDED.""Status"",
                ""RegisteredAt"" = EXCLUDED.""RegisteredAt"";",
            cancellationToken);
    }
}
