using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core <see cref="ISaveChangesInterceptor"/> that writes an audit entry for
/// every tracked <see cref="BaseEntity"/> change (AC-004).
///
/// Actor identity is resolved from the active <see cref="HttpContext"/> claims.
/// Falls back to <c>Guid.Empty</c> / <c>"system"</c> for background-job contexts.
///
/// Audit writes via <see cref="IAuditLogService"/> are best-effort — failures
/// are logged at Warning and do not abort the save.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    // Entity types excluded from audit: reference data (never mutated by the app)
    // and AuditLog itself (avoid recursion with the EF-tracked audit_logs table).
    private static readonly HashSet<Type> ExcludedTypes = [typeof(AuditLog), typeof(InsuranceRecord)];

    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(
        IAuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditSaveChangesInterceptor> logger)
    {
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return result;

        var (actorId, actorRole) = ResolveActor();

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e =>
                e.Entity is BaseEntity &&
                !ExcludedTypes.Contains(e.Entity.GetType()) &&
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            var actionType = entry.State switch
            {
                EntityState.Added    => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted  => "DELETE",
                _                    => "UNKNOWN"
            };

            await _auditLogService.LogAsync(
                actorId,
                actorRole,
                actionType,
                entity.GetType().Name,
                entity.Id,
                cancellationToken: cancellationToken);
        }

        return result;
    }

    private (Guid actorId, string actorRole) ResolveActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
            return (Guid.Empty, "system");

        var idClaim = user.FindFirstValue("sub");
        var actorId = Guid.TryParse(idClaim, out var parsed) ? parsed : Guid.Empty;
        var actorRole = user.FindFirstValue("role") ?? "anonymous";

        return (actorId, actorRole);
    }
}
