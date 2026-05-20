using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using UPACIP.Application.DTOs;
using UPACIP.Application.Interfaces;
using UPACIP.Application.Queries.Admin;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.Repositories;

/// <summary>
/// Read-only repository for <c>audit.audit_log</c>.
/// Uses raw ADO.NET (Npgsql) via <see cref="AuditReadDbContext"/> with the
/// <c>audit_reader</c> role (SELECT-only, AC-006).
/// PHI field values in <c>metadata</c> JSON are replaced with
/// <c>[PHI-REDACTED]</c> for all PHI action types (AC-004, HIPAA §164.312(b)).
/// </summary>
public sealed class AuditLogReadRepository : IAuditLogReadRepository
{
    // ── PHI action types (AC-004) ─────────────────────────────────────────
    internal static readonly HashSet<string> PhiActionTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "PROFILE_VIEWED",
            "DOCUMENT_UPLOADED",
            "INTAKE_SUBMITTED",
            "CODES_VERIFIED",
        };

    // PHI field names that must be masked in metadata JSON (HIPAA §164.514).
    internal static readonly HashSet<string> PhiFieldNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "patientId", "patientName", "firstName", "lastName",
            "dateOfBirth", "dob", "ssn", "mrn",
            "diagnosis", "medication", "treatmentPlan", "insuranceId",
            "phoneNumber", "address", "email",
        };

    // Status-failure pattern (no dedicated column — derived from action_type).
    private const string FailureSuffix1 = "_FAILED";
    private const string FailureSuffix2 = "_FAIL";
    private const string FailureSubstring = "FAILURE";

    // ── Category → ILIKE patterns ─────────────────────────────────────────
    private static readonly Dictionary<string, string[]> CategoryPatterns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTH"] = new[]
            {
                "USER_LOGIN%", "USER_LOGOUT%", "SESSION_%",
                "MFA_%", "PASSWORD_%", "AUTH_%",
            },
            ["ACCOUNT"] = new[]
            {
                "USER_CREATED%", "USER_UPDATED%", "USER_DEACTIVATED%",
                "USER_REACTIVATED%", "ROLE_CHANGED%", "PROFILE_UPDATED%",
            },
            ["SCHEDULING"] = new[]
            {
                "APPOINTMENT_%", "SLOT_%", "WALKIN_%",
                "WAITLIST_%", "CALENDAR_%",
            },
            ["CLINICAL"] = new[]
            {
                "PROFILE_VIEWED%", "DOCUMENT_%", "INTAKE_%",
                "CODES_%", "CLINICAL_%", "EXTRACTED_%",
            },
            ["INTEGRATION"] = new[]
            {
                "INTEGRATION_%", "SYNC_%", "CALENDAR_CONNECTED%",
            },
            ["SECURITY"] = new[]
            {
                "ACCOUNT_LOCKED%", "BRUTE_%", "AUDIT_LOG_%",
            },
        };

    private const string SelectColumns =
        "id, actor_id, actor_role, action_type, target_entity, target_id, timestamp, metadata";

    private const string FromClause = "FROM audit.audit_log";

    private readonly AuditReadDbContext _context;
    private readonly ILogger<AuditLogReadRepository> _logger;

    public AuditLogReadRepository(
        AuditReadDbContext context,
        ILogger<AuditLogReadRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Public interface ──────────────────────────────────────────────────

    public async Task<PagedResult<AuditLogEntryDto>> GetPagedAsync(
        GetAuditLogQuery query,
        CancellationToken ct = default)
    {
        var (whereSql, parameters) = BuildWhereClause(query);

        var countSql = $"SELECT COUNT(*) {FromClause} {whereSql}";
        var dataSql =
            $"""
             SELECT {SelectColumns}
             {FromClause}
             {whereSql}
             ORDER BY timestamp DESC
             OFFSET @offset LIMIT @pageSize
             """;

        try
        {
            await using var connection = _context.CreateConnection();
            await connection.OpenAsync(ct);

            // Single-pass count
            long total;
            await using (var countCmd = new NpgsqlCommand(countSql, connection))
            {
                AddParameters(countCmd, parameters);
                total = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);
            }

            // Data page
            var data = new List<AuditLogEntryDto>();
            await using (var dataCmd = new NpgsqlCommand(dataSql, connection))
            {
                AddParameters(dataCmd, parameters);
                dataCmd.Parameters.AddWithValue("offset", (query.Page - 1) * query.PageSize);
                dataCmd.Parameters.AddWithValue("pageSize", query.PageSize);

                await using var reader = await dataCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    data.Add(MapFromReader(reader));
            }

            return new PagedResult<AuditLogEntryDto>(data, (int)total, query.Page, query.PageSize);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Audit log paged query failed.");
            throw;
        }
    }

    public async Task<AuditLogStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var todayStart = DateTime.UtcNow.Date;

        const string sql =
            """
            SELECT
                COUNT(*)                        FILTER (WHERE timestamp >= @today)                                                       AS events_today,
                COUNT(DISTINCT actor_id)        FILTER (WHERE timestamp >= @today)                                                       AS unique_users,
                COUNT(*)                        FILTER (WHERE timestamp >= @today AND action_type = ANY(@phiActions))                    AS phi_access_events,
                COUNT(*)                        FILTER (WHERE timestamp >= @today AND (action_type ILIKE '%_FAILED' OR action_type ILIKE '%FAILURE%' OR action_type ILIKE '%_FAIL')) AS failed_attempts
            FROM audit.audit_log
            """;

        try
        {
            await using var connection = _context.CreateConnection();
            await connection.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("today", NpgsqlDbType.TimestampTz, todayStart);

            var phiParam = new NpgsqlParameter("phiActions", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = PhiActionTypes.ToArray(),
            };
            cmd.Parameters.Add(phiParam);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new AuditLogStatsDto(0, 0, 0, 0);

            return new AuditLogStatsDto(
                EventsToday: (int)(long)reader["events_today"],
                UniqueUsers: (int)(long)reader["unique_users"],
                PhiAccessEvents: (int)(long)reader["phi_access_events"],
                FailedAttempts: (int)(long)reader["failed_attempts"]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Audit log stats query failed.");
            throw;
        }
    }

    public async IAsyncEnumerable<AuditLogEntryDto> StreamAsync(
        GetAuditLogQuery query,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (whereSql, parameters) = BuildWhereClause(query);

        var sql =
            $"""
             SELECT {SelectColumns}
             {FromClause}
             {whereSql}
             ORDER BY timestamp DESC
             """;

        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, connection);
        AddParameters(cmd, parameters);

        await using var reader = await cmd.ExecuteReaderAsync(
            System.Data.CommandBehavior.SequentialAccess, ct);

        while (await reader.ReadAsync(ct))
            yield return MapFromReader(reader);
    }

    // ── WHERE clause builder ──────────────────────────────────────────────

    private static (string whereSql, IReadOnlyList<(string Name, object Value, NpgsqlDbType? DbType)> parameters)
        BuildWhereClause(GetAuditLogQuery query)
    {
        var conditions = new List<string>();
        var parameters = new List<(string, object, NpgsqlDbType?)>();

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            conditions.Add("timestamp >= @dateFrom");
            parameters.Add(("dateFrom", from, NpgsqlDbType.TimestampTz));
        }

        if (query.DateTo.HasValue)
        {
            // Exclusive upper bound: include the full day of DateTo.
            var to = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            conditions.Add("timestamp < @dateTo");
            parameters.Add(("dateTo", to, NpgsqlDbType.TimestampTz));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            conditions.Add("actor_role = @role");
            parameters.Add(("role", query.Role, null));
        }

        if (!string.IsNullOrWhiteSpace(query.ActionCategory)
            && CategoryPatterns.TryGetValue(query.ActionCategory, out var patterns)
            && patterns.Length > 0)
        {
            conditions.Add("action_type ILIKE ANY(@categoryPatterns)");
            parameters.Add(("categoryPatterns", patterns, NpgsqlDbType.Array | NpgsqlDbType.Text));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (query.Status.Equals("FAILURE", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add(
                    "(action_type ILIKE '%_FAILED' OR action_type ILIKE '%FAILURE%' OR action_type ILIKE '%_FAIL')");
            }
            else if (query.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add(
                    "NOT (action_type ILIKE '%_FAILED' OR action_type ILIKE '%FAILURE%' OR action_type ILIKE '%_FAIL')");
            }
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            // Escape wildcards to prevent accidental LIKE expansion.
            var escaped = EscapeLike(query.SearchText);
            var searchLike = $"%{escaped}%";
            conditions.Add(
                "(actor_role ILIKE @search OR action_type ILIKE @search " +
                "OR target_entity ILIKE @search OR metadata ILIKE @search)");
            parameters.Add(("search", searchLike, null));
        }

        var whereSql = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        return (whereSql, parameters);
    }

    // ── ADO.NET helpers ───────────────────────────────────────────────────

    private static void AddParameters(
        NpgsqlCommand cmd,
        IReadOnlyList<(string Name, object Value, NpgsqlDbType? DbType)> parameters)
    {
        foreach (var (name, value, dbType) in parameters)
        {
            if (dbType.HasValue)
            {
                var p = new NpgsqlParameter(name, dbType.Value) { Value = value };
                cmd.Parameters.Add(p);
            }
            else
            {
                cmd.Parameters.AddWithValue(name, value);
            }
        }
    }

    // ── Row mapper ────────────────────────────────────────────────────────

    private static AuditLogEntryDto MapFromReader(NpgsqlDataReader reader)
    {
        var eventId = reader.GetGuid(0).ToString("D");
        var actorId = reader.IsDBNull(1) ? string.Empty : reader.GetGuid(1).ToString("D");
        var actorRole = reader.GetString(2);
        var actionType = reader.GetString(3);
        var targetEntity = reader.GetString(4);
        var targetId = reader.IsDBNull(5) ? null : reader.GetGuid(5).ToString("D");
        var timestamp = reader.GetFieldValue<DateTimeOffset>(6);
        var metadata = reader.IsDBNull(7) ? null : reader.GetString(7);

        var phiAccess = IsPhiAction(actionType);
        var status = DeriveStatus(actionType);
        var category = DeriveCategory(actionType);

        var (actorName, actorEmail, ipAddress, userAgent, correlationId, sessionId, maskedPayload)
            = ExtractAndMaskMetadata(metadata, phiAccess);

        return new AuditLogEntryDto(
            EventId: eventId,
            CorrelationId: correlationId,
            SessionId: sessionId,
            Timestamp: timestamp,
            ActorId: actorId,
            ActorName: actorName,
            ActorEmail: actorEmail,
            ActorRole: actorRole,
            IpAddress: ipAddress,
            UserAgent: userAgent,
            ActionType: actionType,
            ActionCategory: category,
            Resource: targetEntity,
            TargetId: targetId,
            PayloadJson: maskedPayload,
            Status: status,
            PhiAccess: phiAccess
        );
    }

    // ── Derivation helpers (internal for unit testing) ────────────────────

    /// <summary>Returns <see langword="true"/> for action types that access PHI.</summary>
    internal static bool IsPhiAction(string actionType)
        => PhiActionTypes.Contains(actionType);

    /// <summary>Derives "Success" or "Failure" from the action type string.</summary>
    internal static string DeriveStatus(string actionType)
    {
        var t = actionType.ToUpperInvariant();
        return t.EndsWith(FailureSuffix1) || t.EndsWith(FailureSuffix2) || t.Contains(FailureSubstring)
            ? "Failure"
            : "Success";
    }

    /// <summary>Maps an action type to its HIPAA audit category.</summary>
    internal static string DeriveCategory(string actionType)
    {
        var t = actionType.ToUpperInvariant();

        if (t.StartsWith("USER_LOGIN") || t.StartsWith("USER_LOGOUT")
            || t.StartsWith("SESSION_") || t.StartsWith("MFA_")
            || t.StartsWith("PASSWORD_") || t.StartsWith("AUTH_"))
            return "AUTH";

        if (t.StartsWith("USER_") || t.StartsWith("ROLE_"))
            return "ACCOUNT";

        if (t.StartsWith("APPOINTMENT_") || t.StartsWith("SLOT_")
            || t.StartsWith("WALKIN_") || t.StartsWith("WAITLIST_")
            || t.StartsWith("CALENDAR_"))
            return "SCHEDULING";

        if (t is "PROFILE_VIEWED" || t.StartsWith("DOCUMENT_")
            || t.StartsWith("INTAKE_") || t.StartsWith("CODES_")
            || t.StartsWith("CLINICAL_") || t.StartsWith("EXTRACTED_"))
            return "CLINICAL";

        if (t.StartsWith("AUDIT_LOG_"))
            return "SECURITY";

        if (t.StartsWith("INTEGRATION_") || t.StartsWith("SYNC_"))
            return "INTEGRATION";

        return "SECURITY";
    }

    /// <summary>
    /// Parses the <c>metadata</c> JSON, extracts well-known fields,
    /// and replaces PHI field values with <c>[PHI-REDACTED]</c> when
    /// <paramref name="phiAccess"/> is <see langword="true"/> (AC-004).
    /// </summary>
    internal static (
        string actorName,
        string actorEmail,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        string? sessionId,
        string? maskedPayload)
        ExtractAndMaskMetadata(string? metadata, bool phiAccess)
    {
        if (string.IsNullOrEmpty(metadata))
            return (string.Empty, string.Empty, null, null, null, null, null);

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            var root = doc.RootElement;

            var actorName = GetStringField(root, "actorName");
            var actorEmail = GetStringField(root, "actorEmail");
            var ipAddress = GetStringField(root, "ipAddress");
            var userAgent = GetStringField(root, "userAgent");
            var correlationId = GetStringField(root, "correlationId");
            var sessionId = GetStringField(root, "sessionId");

            var maskedPayload = phiAccess ? MaskPhiFields(metadata) : metadata;

            return (actorName ?? string.Empty, actorEmail ?? string.Empty,
                ipAddress, userAgent, correlationId, sessionId, maskedPayload);
        }
        catch (JsonException)
        {
            // Malformed metadata — return as-is without masking.
            return (string.Empty, string.Empty, null, null, null, null, metadata);
        }
    }

    /// <summary>
    /// Returns a copy of <paramref name="json"/> with all PHI field values
    /// replaced by <c>[PHI-REDACTED]</c>. Keys whose names appear in
    /// <see cref="PhiFieldNames"/> are masked regardless of nesting depth
    /// at the top level.
    /// </summary>
    internal static string MaskPhiFields(string json)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (raw is null) return json;

            var masked = new Dictionary<string, object?>(raw.Count);
            foreach (var (key, value) in raw)
            {
                masked[key] = PhiFieldNames.Contains(key)
                    ? (object?)"[PHI-REDACTED]"
                    : value;
            }
            return JsonSerializer.Serialize(masked);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string? GetStringField(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static string EscapeLike(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
