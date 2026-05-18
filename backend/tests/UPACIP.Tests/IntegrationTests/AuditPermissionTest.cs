using Npgsql;

namespace UPACIP.Tests.IntegrationTests;

/// <summary>
/// Asserts that the application database role is restricted to INSERT and SELECT
/// on <c>audit.audit_log</c>; an UPDATE attempt must return PostgreSQL error
/// <c>42501</c> (insufficient_privilege) — AC-003.
///
/// Prerequisites (why this test may be skipped):
///   1. <c>ConnectionStrings__DefaultConnection</c> environment variable must be set.
///   2. <c>scripts/audit-schema.sql</c> must have been executed against the target DB,
///      with <c>&lt;app_role&gt;</c> replaced by the role used in the connection string.
///   3. The connecting role must NOT be a PostgreSQL superuser — superusers bypass
///      GRANT/REVOKE and would make this test impossible to assert.
///
/// In CI, set <c>ConnectionStrings__DefaultConnection</c> as a secret and ensure
/// the service account is a non-superuser role with INSERT/SELECT only.
/// </summary>
public sealed class AuditPermissionTest
{
    private const string ConnEnvVar = "ConnectionStrings__DefaultConnection";

    private const string UpdateSql =
        "UPDATE audit.audit_log SET actor_role = 'permission-check' WHERE false";

    private const string InsertSql =
        """
        INSERT INTO audit.audit_log (actor_role, action_type, target_entity)
        VALUES ('test', 'PERMISSION_CHECK', 'AuditPermissionTest')
        """;

    private const string CleanupSql =
        "DELETE FROM audit.audit_log WHERE action_type = 'PERMISSION_CHECK'";

    /// <summary>
    /// A non-superuser application role must receive <c>42501</c> when it
    /// attempts UPDATE on <c>audit.audit_log</c> (AC-003).
    /// </summary>
    [Fact]
    public async Task AuditLog_Update_By_AppRole_Returns_PermissionDenied_42501()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnEnvVar);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // No DB available in this environment — skip gracefully.
            // CI must set the env var for this gate to be enforced.
            return;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Abort if the current session is a superuser — superusers bypass GRANT
        // restrictions so the 42501 test would be a false negative.
        var isSuperuser = await IsSuperuserAsync(connection);
        if (isSuperuser)
        {
            // This test is intentionally inconclusive for superuser sessions.
            // Use a dedicated non-superuser application role for meaningful CI gating.
            return;
        }

        // Must fail with 42501 (insufficient_privilege).
        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var cmd = new NpgsqlCommand(UpdateSql, connection);
            await cmd.ExecuteNonQueryAsync();
        });

        Assert.Equal("42501", ex.SqlState);
    }

    /// <summary>
    /// The application role must be able to INSERT into <c>audit.audit_log</c> (AC-003).
    /// </summary>
    [Fact]
    public async Task AuditLog_Insert_By_AppRole_Succeeds()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnEnvVar);
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var insertCmd = new NpgsqlCommand(InsertSql, connection);
        var rows = await insertCmd.ExecuteNonQueryAsync();
        Assert.Equal(1, rows);

        // Cleanup: best-effort; may fail if role lacks DELETE (acceptable — test already passed).
        try
        {
            await using var cleanCmd = new NpgsqlCommand(CleanupSql, connection);
            await cleanCmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException) { /* permission denied on DELETE is expected for restricted role */ }
    }

    private static async Task<bool> IsSuperuserAsync(NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT usesuper FROM pg_user WHERE usename = current_user", connection);
        var result = await cmd.ExecuteScalarAsync();
        return result is true;
    }
}
