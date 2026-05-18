using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Audit;

/// <summary>
/// Writes immutable audit entries to <c>audit.audit_log</c> via raw ADO.NET,
/// intentionally bypassing EF Core to:
/// <list type="bullet">
///   <item>Prevent recursive interceptor invocations</item>
///   <item>Enforce the INSERT-only database role (DR-003, NFR-007)</item>
/// </list>
/// The connection string is taken from <c>ConnectionStrings:DefaultConnection</c>.
/// A failure here is logged as a warning and does NOT bubble up to the caller —
/// the audit schema may not be provisioned yet during development (depends on
/// task_002_database-audit-schema).
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private const string InsertSql =
        """
        INSERT INTO audit.audit_log (actor_id, actor_role, action_type, target_entity, target_id, metadata)
        VALUES (@actorId, @actorRole, @actionType, @targetEntity, @targetId, @metadata)
        """;

    private readonly string _connectionString;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IConfiguration configuration, ILogger<AuditLogService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string is not configured.");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        Guid actorId,
        string actorRole,
        string actionType,
        string targetEntity,
        Guid targetId,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(InsertSql, connection);
            cmd.Parameters.AddWithValue("actorId", actorId == Guid.Empty ? DBNull.Value : actorId);
            cmd.Parameters.AddWithValue("actorRole", actorRole);
            cmd.Parameters.AddWithValue("actionType", actionType);
            cmd.Parameters.AddWithValue("targetEntity", targetEntity);
            cmd.Parameters.AddWithValue("targetId", targetId == Guid.Empty ? DBNull.Value : targetId);
            cmd.Parameters.AddWithValue("metadata", string.IsNullOrEmpty(metadata) ? DBNull.Value : (object)metadata);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit failures must never abort the main operation (HIPAA — availability over audit perfection).
            // Log at Warning so ops teams are alerted without surfacing to the caller.
            _logger.LogWarning(
                ex,
                "Audit log write failed for {ActionType} on {TargetEntity} {TargetId} by actor {ActorId}.",
                actionType, targetEntity, targetId, actorId);
        }
    }
}
