using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Handlers.Queue;
using UPACIP.Application.Queries.Queue;
using AppValidationException = UPACIP.Application.Exceptions.ValidationException;

namespace UPACIP.API.Controllers;

/// <summary>
/// Staff queue management endpoints.
/// All routes require <c>StaffPolicy</c> (role = "Staff") — patients calling
/// these endpoints receive HTTP 403 (OWASP A01 — Broken Access Control, AC-005).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffPolicy")]
[Route("api/v{version:apiVersion}/queue")]
public sealed class QueueController : ControllerBase
{
    private readonly GetTodaysQueueHandler _getTodaysQueueHandler;
    private readonly ArriveQueueEntryHandler _arriveHandler;
    private readonly ReorderQueueEntryHandler _reorderHandler;
    private readonly RemoveQueueEntryHandler _removeHandler;

    public QueueController(
        GetTodaysQueueHandler getTodaysQueueHandler,
        ArriveQueueEntryHandler arriveHandler,
        ReorderQueueEntryHandler reorderHandler,
        RemoveQueueEntryHandler removeHandler)
    {
        _getTodaysQueueHandler = getTodaysQueueHandler;
        _arriveHandler         = arriveHandler;
        _reorderHandler        = reorderHandler;
        _removeHandler         = removeHandler;
    }

    /// <summary>
    /// Returns all today's appointments in chronological/display order.
    /// </summary>
    [HttpGet("today")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaysQueue(CancellationToken cancellationToken)
    {
        var queue = await _getTodaysQueueHandler.HandleAsync(
            new GetTodaysQueueQuery(DateOnly.FromDateTime(DateTime.UtcNow)),
            cancellationToken);

        return Ok(queue);
    }

    /// <summary>
    /// Marks the appointment as "Arrived". Returns HTTP 409 if already arrived.
    /// </summary>
    [HttpPatch("{id:guid}/arrive")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkArrived(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var staffId = ResolveStaffId();
            await _arriveHandler.HandleAsync(new ArriveQueueEntryCommand(id, staffId), cancellationToken);
            return NoContent();
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (AppValidationException ex)
        {
            return UnprocessableEntity(new { message = "Validation failed.", errors = ex.Errors });
        }
    }

    /// <summary>
    /// Updates the display order of a queue entry. Always saves.
    /// Returns <c>slotConflict: true</c> in the body when a time-window conflict is detected.
    /// </summary>
    [HttpPatch("{id:guid}/reorder")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reorder(
        [FromRoute] Guid id,
        [FromBody] ReorderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var staffId = ResolveStaffId();
            var result = await _reorderHandler.HandleAsync(
                new ReorderQueueEntryCommand(id, request.NewIndex, staffId),
                cancellationToken);

            return Ok(new { slotConflict = result.SlotConflict });
        }
        catch (AppValidationException ex)
        {
            return UnprocessableEntity(new { message = "Validation failed.", errors = ex.Errors });
        }
    }

    /// <summary>
    /// Removes a queue entry with a mandatory reason. Returns HTTP 422 if reason is absent.
    /// Accepts removal even if the patient is already "Arrived" (override semantics).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveFromQueue(
        [FromRoute] Guid id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return UnprocessableEntity(new { message = "Query parameter 'reason' is required." });

        try
        {
            var staffId = ResolveStaffId();
            await _removeHandler.HandleAsync(
                new RemoveQueueEntryCommand(id, reason, staffId),
                cancellationToken);

            return NoContent();
        }
        catch (AppValidationException ex)
        {
            return UnprocessableEntity(new { message = "Validation failed.", errors = ex.Errors });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid ResolveStaffId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>Request body for <c>PATCH /queue/{id}/reorder</c>.</summary>
public sealed record ReorderRequest(int NewIndex);
