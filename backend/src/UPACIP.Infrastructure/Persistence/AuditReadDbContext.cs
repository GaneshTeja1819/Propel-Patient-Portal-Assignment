using Npgsql;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// Provides read-only connections to the <c>audit.audit_log</c> table using
/// the <c>audit_reader</c> PostgreSQL role (SELECT-only on <c>audit.*</c> — AC-006).
/// Uses the <c>AuditReadConnection</c> configuration key; falls back to
/// <c>DefaultConnection</c> when absent (development convenience).
/// Injected as a keyed service in DI to prevent accidental write-role substitution.
/// </summary>
public sealed class AuditReadDbContext
{
    private readonly string _connectionString;

    public AuditReadDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "AuditReadConnection (or DefaultConnection fallback) is required.", nameof(connectionString));

        _connectionString = connectionString;
    }

    /// <summary>Opens a new read-only connection. Caller is responsible for disposal.</summary>
    public NpgsqlConnection CreateConnection() => new(_connectionString);
}
