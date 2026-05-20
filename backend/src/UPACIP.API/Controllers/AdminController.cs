using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Application.Commands.Admin;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Handlers.Admin;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.API.Controllers;

/// <summary>
/// Admin-only endpoints for user management (SCR-015 / EP-007 / us_024).
/// All routes require <c>AdminPolicy</c> which maps to <c>role = "Admin"</c> in the JWT.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "AdminPolicy")]
[Route("api/v{version:apiVersion}/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminUserStore _userStore;
    private readonly CreateUserHandler _createUserHandler;
    private readonly UpdateUserHandler _updateUserHandler;
    private readonly ChangeRoleHandler _changeRoleHandler;
    private readonly DeactivateUserHandler _deactivateUserHandler;

    public AdminController(
        IAdminUserStore userStore,
        CreateUserHandler createUserHandler,
        UpdateUserHandler updateUserHandler,
        ChangeRoleHandler changeRoleHandler,
        DeactivateUserHandler deactivateUserHandler)
    {
        _userStore = userStore;
        _createUserHandler = createUserHandler;
        _updateUserHandler = updateUserHandler;
        _changeRoleHandler = changeRoleHandler;
        _deactivateUserHandler = deactivateUserHandler;
    }

    // ── Search / list ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns users matching the optional search query.
    /// Performs case-insensitive ILIKE search on full name and email (AC-001).
    /// </summary>
    [HttpGet("users")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var adminId = ResolveActorId();
        var users = await _userStore.SearchUsersAsync(q, cancellationToken);

        var result = users.Select(u => MapToDto(u, adminId)).ToList();
        return Ok(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    /// <summary>Creates a new user with the specified role (AC-001).</summary>
    [HttpPost("users")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var newId = await _createUserHandler.HandleAsync(
                new CreateUserCommand(
                    ResolveActorId(),
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.Role),
                cancellationToken);

            return CreatedAtAction(nameof(GetUsers), new { }, new CreateUserResponse(newId));
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Application.Exceptions.ValidationException ex)
        {
            return UnprocessableEntity(new { errors = ex.Errors });
        }
    }

    // ── Update profile ────────────────────────────────────────────────────────

    /// <summary>Updates a user's profile fields (AC-001).</summary>
    [HttpPatch("users/{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _updateUserHandler.HandleAsync(
                new UpdateUserCommand(
                    ResolveActorId(),
                    id,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.PhoneNumber),
                cancellationToken);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    /// <summary>
    /// Deactivates the target user, invalidates their sessions, and writes an audit entry.
    /// Returns HTTP 422 if the admin attempts to deactivate their own account (AC-003).
    /// </summary>
    [HttpPatch("users/{id:guid}/deactivate")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _deactivateUserHandler.DeactivateAsync(ResolveActorId(), id, cancellationToken);
            return NoContent();
        }
        catch (Application.Exceptions.ValidationException ex)
        {
            return UnprocessableEntity(new { errors = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ── Reactivate ────────────────────────────────────────────────────────────

    /// <summary>Reactivates a previously deactivated user and writes an audit entry.</summary>
    [HttpPatch("users/{id:guid}/reactivate")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _deactivateUserHandler.ReactivateAsync(ResolveActorId(), id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ── Change role ────────────────────────────────────────────────────────────

    /// <summary>
    /// Changes the role of a target user (AC-002, AC-005).
    /// Invalidates all active sessions so the new role is enforced on next login.
    /// </summary>
    [HttpPatch("users/{id:guid}/role")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _changeRoleHandler.HandleAsync(
                ResolveActorId(), id, request.Role, cancellationToken);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Application.Exceptions.ValidationException ex)
        {
            return UnprocessableEntity(new { errors = ex.Errors });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid ResolveActorId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static UserSummaryDto MapToDto(User u, Guid currentAdminId) =>
        new(
            Id: u.Id,
            Name: $"{u.FirstName} {u.LastName}".Trim(),
            Email: u.Email,
            Role: u.Role,
            Status: u.IsActive ? "Active" : "Inactive",
            LastLogin: null,   // LastLogin not stored on User entity; reserved for future.
            IsSelf: u.Id == currentAdminId);

    // ── Request / Response DTOs ───────────────────────────────────────────────

    public sealed record CreateUserRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role);

    public sealed record UpdateUserRequest(
        string? FirstName,
        string? LastName,
        string? Email,
        string? PhoneNumber);

    public sealed record ChangeRoleRequest(string Role);

    public sealed record CreateUserResponse(Guid Id);

    public sealed record UserSummaryDto(
        Guid Id,
        string Name,
        string Email,
        string Role,
        string Status,
        string? LastLogin,
        bool IsSelf);
}
