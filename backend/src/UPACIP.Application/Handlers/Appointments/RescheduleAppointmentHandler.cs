using Microsoft.Extensions.Logging;
using System.Text.Json;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Appointments;

public enum RescheduleAppointmentStatus
{
    Success,
    NotFound,
    Conflict
}

public sealed class RescheduleAppointmentResult
{
    public RescheduleAppointmentStatus Status { get; init; }
    public Guid? OldSlotId { get; init; }
    public Guid? NewSlotId { get; init; }
}

/// <summary>
/// Handles atomic reschedule updates for appointment slots.
/// </summary>
public sealed class RescheduleAppointmentHandler
{
    private readonly IAppointmentManagementStore _managementStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlotCacheService _slotCacheService;
    private readonly IAuditLogService _auditLogService;
    private readonly ISlotSwapJobEnqueuer _slotSwapJobEnqueuer;
    private readonly IPdfConfirmationJobEnqueuer _pdfJobEnqueuer;
    private readonly IReminderJobEnqueuer _reminderJobEnqueuer;
    private readonly ICalendarSyncService _calendarSyncService;
    private readonly ILogger<RescheduleAppointmentHandler> _logger;

    public RescheduleAppointmentHandler(
        IAppointmentManagementStore managementStore,
        IUnitOfWork unitOfWork,
        ISlotCacheService slotCacheService,
        IAuditLogService auditLogService,
        ISlotSwapJobEnqueuer slotSwapJobEnqueuer,
        IPdfConfirmationJobEnqueuer pdfJobEnqueuer,
        IReminderJobEnqueuer reminderJobEnqueuer,
        ICalendarSyncService calendarSyncService,
        ILogger<RescheduleAppointmentHandler> logger)
    {
        _managementStore     = managementStore;
        _unitOfWork          = unitOfWork;
        _slotCacheService    = slotCacheService;
        _auditLogService     = auditLogService;
        _slotSwapJobEnqueuer = slotSwapJobEnqueuer;
        _pdfJobEnqueuer      = pdfJobEnqueuer;
        _reminderJobEnqueuer = reminderJobEnqueuer;
        _calendarSyncService = calendarSyncService;
        _logger              = logger;
    }

    public async Task<RescheduleAppointmentResult> HandleAsync(
        RescheduleAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _managementStore.GetAppointmentWithSlotAsync(
            command.AppointmentId,
            command.PatientId,
            cancellationToken);

        if (appointment is null)
        {
            return new RescheduleAppointmentResult { Status = RescheduleAppointmentStatus.NotFound };
        }

        var newSlot = await _managementStore.GetSlotByIdAsync(command.NewSlotId, cancellationToken);
        if (newSlot is null || !newSlot.IsAvailable)
        {
            return new RescheduleAppointmentResult { Status = RescheduleAppointmentStatus.Conflict };
        }

        var oldSlotId = appointment.SlotId;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            appointment.Slot.IsAvailable = true;
            newSlot.IsAvailable = false;
            appointment.SlotId = newSlot.Id;
            appointment.ProviderId = newSlot.ProviderId;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogInformation(ex, "Reschedule conflict for appointment {AppointmentId}.", appointment.Id);
            return new RescheduleAppointmentResult { Status = RescheduleAppointmentStatus.Conflict };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await _slotCacheService.InvalidateSlotAsync(oldSlotId.ToString(), cancellationToken);
        await _slotCacheService.InvalidateSlotAsync(newSlot.Id.ToString(), cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                actorId: command.PatientId,
                actorRole: "Patient",
                actionType: "APPOINTMENT_RESCHEDULED",
                targetEntity: "Appointment",
                targetId: appointment.Id,
                metadata: $"{{\"oldSlotId\":\"{oldSlotId}\",\"newSlotId\":\"{newSlot.Id}\"}}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log failed for rescheduled appointment {AppointmentId}.", appointment.Id);
        }

        _pdfJobEnqueuer.Enqueue(appointment.Id, isReschedule: true);
        _slotSwapJobEnqueuer.Enqueue(oldSlotId);

        // US_020: Cancel old reminder jobs; enqueue new ones for the updated appointment time (edge case)
        try
        {
            if (!string.IsNullOrWhiteSpace(appointment.ReminderJobIds))
            {
                var oldJobIds = JsonSerializer.Deserialize<List<string>>(appointment.ReminderJobIds) ?? [];
                _reminderJobEnqueuer.Cancel(oldJobIds);
            }

            var newJobIds = _reminderJobEnqueuer.Schedule(appointment.Id, newSlot.StartTime);
            if (newJobIds.Count > 0)
            {
                appointment.ReminderJobIds = JsonSerializer.Serialize(newJobIds);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reminder job rescheduling failed for {AppointmentId}; reschedule preserved.", appointment.Id);
        }

        // US_021, AC-002: Update calendar event non-blocking (failure must not roll back reschedule)
        _ = Task.Run(async () =>
        {
            try
            {
                await _calendarSyncService.UpdateEventAsync(appointment.Id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Calendar sync update failed for rescheduled appointment {AppointmentId}.", appointment.Id);
            }
        });

        return new RescheduleAppointmentResult
        {
            Status = RescheduleAppointmentStatus.Success,
            OldSlotId = oldSlotId,
            NewSlotId = newSlot.Id,
        };
    }

    private static bool IsConcurrencyException(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (string.Equals(current.GetType().Name, "DbUpdateConcurrencyException", StringComparison.Ordinal))
                return true;

            current = current.InnerException!;
        }

        return false;
    }
}
