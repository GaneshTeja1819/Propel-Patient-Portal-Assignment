using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Appointments;

public enum CancelAppointmentStatus
{
    Success,
    NotFound,
    PastAppointment
}

public sealed class CancelAppointmentResult
{
    public CancelAppointmentStatus Status { get; init; }
    public Guid? SlotId { get; init; }
}

/// <summary>
/// Handles confirmed appointment cancellations for patients.
/// </summary>
public sealed class CancelAppointmentHandler
{
    private readonly IAppointmentManagementStore _managementStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlotCacheService _slotCacheService;
    private readonly IAuditLogService _auditLogService;
    private readonly ISlotSwapJobEnqueuer _slotSwapJobEnqueuer;
    private readonly ICalendarSyncService _calendarSyncService;
    private readonly ILogger<CancelAppointmentHandler> _logger;

    public CancelAppointmentHandler(
        IAppointmentManagementStore managementStore,
        IUnitOfWork unitOfWork,
        ISlotCacheService slotCacheService,
        IAuditLogService auditLogService,
        ISlotSwapJobEnqueuer slotSwapJobEnqueuer,
        ICalendarSyncService calendarSyncService,
        ILogger<CancelAppointmentHandler> logger)
    {
        _managementStore     = managementStore;
        _unitOfWork          = unitOfWork;
        _slotCacheService    = slotCacheService;
        _auditLogService     = auditLogService;
        _slotSwapJobEnqueuer = slotSwapJobEnqueuer;
        _calendarSyncService = calendarSyncService;
        _logger              = logger;
    }

    public async Task<CancelAppointmentResult> HandleAsync(
        CancelAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _managementStore.GetAppointmentWithSlotAsync(
            command.AppointmentId,
            command.PatientId,
            cancellationToken);

        if (appointment is null)
        {
            return new CancelAppointmentResult { Status = CancelAppointmentStatus.NotFound };
        }

        if (appointment.Slot.StartTime <= DateTimeOffset.UtcNow)
        {
            return new CancelAppointmentResult { Status = CancelAppointmentStatus.PastAppointment };
        }

        appointment.Status = "Cancelled";
        appointment.UpdatedAt = DateTimeOffset.UtcNow;
        appointment.Slot.IsAvailable = true;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _slotCacheService.InvalidateSlotAsync(appointment.SlotId.ToString(), cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                actorId: command.PatientId,
                actorRole: "Patient",
                actionType: "APPOINTMENT_CANCELLED",
                targetEntity: "Appointment",
                targetId: appointment.Id,
                metadata: $"{{\"slotId\":\"{appointment.SlotId}\"}}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log failed for cancelled appointment {AppointmentId}.", appointment.Id);
        }

        _slotSwapJobEnqueuer.Enqueue(appointment.SlotId);

        // US_021, AC-003: Delete calendar event non-blocking (failure must not affect cancellation)
        var appointmentId = appointment.Id;
        _ = Task.Run(async () =>
        {
            try
            {
                await _calendarSyncService.DeleteEventAsync(appointmentId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Calendar sync delete failed for cancelled appointment {AppointmentId}.", appointmentId);
            }
        });

        return new CancelAppointmentResult
        {
            Status = CancelAppointmentStatus.Success,
            SlotId = appointment.SlotId,
        };
    }
}
