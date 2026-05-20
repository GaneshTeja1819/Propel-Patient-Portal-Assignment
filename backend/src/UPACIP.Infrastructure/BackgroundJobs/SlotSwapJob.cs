using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Evaluates waitlist entries for a released slot and performs an atomic swap
/// for the first eligible entry in FIFO order.
/// </summary>
public sealed class SlotSwapJob
{
    private const int MaxConcurrencyAttempts = 2;

    private readonly AppDbContext _dbContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SlotSwapJob> _logger;

    public SlotSwapJob(
        AppDbContext dbContext,
        IBackgroundJobClient backgroundJobClient,
        IAuditLogService auditLogService,
        ILogger<SlotSwapJob> logger)
    {
        _dbContext = dbContext;
        _backgroundJobClient = backgroundJobClient;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid releasedSlotId, CancellationToken cancellationToken)
    {
        var lockKey = $"slot-swap-{releasedSlotId}";

        try
        {
            using var connection = JobStorage.Current.GetConnection();
            using var distributedLock = connection.AcquireDistributedLock(lockKey, TimeSpan.FromSeconds(5));

            var candidateEntries = await _dbContext.WaitlistEntries
                .AsNoTracking()
                .Where(w => w.Status == "Pending" && w.PreferredSlotId == releasedSlotId)
                .OrderBy(w => w.RegisteredAt)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);

            if (candidateEntries.Count == 0)
            {
                _logger.LogInformation("No waitlist entries for released slot {SlotId}; no-op.", releasedSlotId);
                return;
            }

            foreach (var waitlistEntryId in candidateEntries)
            {
                var swapResult = await TrySwapForEntryAsync(
                    releasedSlotId,
                    waitlistEntryId,
                    cancellationToken);

                if (swapResult.Success && swapResult.AppointmentId.HasValue)
                {
                    _backgroundJobClient.Enqueue<SlotSwapNotificationJob>(
                        job => job.ExecuteAsync(
                            swapResult.AppointmentId.Value,
                            releasedSlotId,
                            null,
                            CancellationToken.None));

                    _logger.LogInformation("Slot swap succeeded for released slot {SlotId}.", releasedSlotId);
                    return;
                }
            }
        }
        catch (DistributedLockTimeoutException ex)
        {
            _logger.LogInformation(
                ex,
                "Slot swap lock timeout for {SlotId}; skipping duplicate evaluation.",
                releasedSlotId);
        }
    }

    private async Task<SwapResult> TrySwapForEntryAsync(
        Guid releasedSlotId,
        Guid waitlistEntryId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var waitlistEntry = await _dbContext.WaitlistEntries
                    .FirstOrDefaultAsync(w => w.Id == waitlistEntryId, cancellationToken);

                if (waitlistEntry is null || waitlistEntry.Status != "Pending")
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return SwapResult.Failed;
                }

                var releasedSlot = await _dbContext.AppointmentSlots
                    .FirstOrDefaultAsync(s => s.Id == releasedSlotId, cancellationToken);

                if (releasedSlot is null || !releasedSlot.IsAvailable)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return SwapResult.Failed;
                }

                var appointment = await _dbContext.Appointments
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == waitlistEntry.AppointmentId, cancellationToken);

                if (appointment is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return SwapResult.Failed;
                }

                var oldSlot = appointment.Slot;

                releasedSlot.IsAvailable = false;
                oldSlot.IsAvailable = true;
                appointment.SlotId = releasedSlotId;
                appointment.ProviderId = releasedSlot.ProviderId;
                appointment.UpdatedAt = DateTimeOffset.UtcNow;

                _dbContext.WaitlistEntries.Remove(waitlistEntry);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new SwapResult(true, appointment.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                if (attempt < MaxConcurrencyAttempts)
                {
                    _logger.LogInformation(
                        ex,
                        "Slot swap concurrency conflict for waitlist entry {WaitlistEntryId}; retrying.",
                        waitlistEntryId);
                    continue;
                }

                await WriteSwapFailureAuditAsync(waitlistEntryId, releasedSlotId, ex, cancellationToken);
                return SwapResult.Failed;
            }
        }

        return SwapResult.Failed;
    }

    private readonly record struct SwapResult(bool Success, Guid? AppointmentId)
    {
        public static SwapResult Failed => new(false, null);
    }

    private async Task WriteSwapFailureAuditAsync(
        Guid waitlistEntryId,
        Guid releasedSlotId,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var entry = await _dbContext.WaitlistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == waitlistEntryId, cancellationToken);

        if (entry is null)
            return;

        try
        {
            await _auditLogService.LogAsync(
                actorId: entry.PatientId,
                actorRole: "Patient",
                actionType: "SLOT_SWAP_FAILED",
                targetEntity: "AppointmentSlot",
                targetId: releasedSlotId,
                metadata: $"{{\"waitlistEntryId\":\"{waitlistEntryId}\",\"error\":\"{ex.GetType().Name}\"}}",
                cancellationToken: cancellationToken);
        }
        catch (Exception auditEx)
        {
            _logger.LogWarning(auditEx, "Audit log failed for slot swap failure {WaitlistEntryId}.", waitlistEntryId);
        }
    }
}
