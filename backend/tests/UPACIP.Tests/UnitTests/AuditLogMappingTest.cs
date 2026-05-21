using System.Text.Json;
using UPACIP.Infrastructure.Repositories;

namespace UPACIP.Tests.UnitTests;

/// <summary>
/// Unit tests for PHI masking, status derivation, and category derivation logic
/// in <see cref="AuditLogReadRepository"/>.
/// These tests verify AC-004 (PHI field redaction) and HIPAA §164.312(b) compliance
/// without requiring a live database connection.
/// </summary>
public sealed class AuditLogMappingTest
{
    // ── PHI action detection ──────────────────────────────────────────────────

    [Theory]
    [InlineData("PROFILE_VIEWED", true)]
    [InlineData("DOCUMENT_UPLOADED", true)]
    [InlineData("INTAKE_SUBMITTED", true)]
    [InlineData("CODES_VERIFIED", true)]
    [InlineData("USER_LOGIN", false)]
    [InlineData("APPOINTMENT_BOOKED", false)]
    [InlineData("AUDIT_LOG_EXPORTED", false)]
    public void IsPhiAction_ReturnsExpected(string actionType, bool expected)
    {
        Assert.Equal(expected, AuditLogReadRepository.IsPhiAction(actionType));
    }

    // ── Status derivation ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("USER_LOGIN", "Success")]
    [InlineData("APPOINTMENT_BOOKED", "Success")]
    [InlineData("USER_LOGIN_FAILED", "Failure")]
    [InlineData("LOGIN_FAIL", "Failure")]
    [InlineData("AUTHENTICATION_FAILURE", "Failure")]
    [InlineData("PROFILE_VIEWED", "Success")]
    public void DeriveStatus_ReturnsExpected(string actionType, string expected)
    {
        Assert.Equal(expected, AuditLogReadRepository.DeriveStatus(actionType));
    }

    // ── Category derivation ───────────────────────────────────────────────────

    [Theory]
    [InlineData("USER_LOGIN", "AUTH")]
    [InlineData("USER_LOGOUT", "AUTH")]
    [InlineData("SESSION_TIMEOUT", "AUTH")]
    [InlineData("USER_CREATED", "ACCOUNT")]
    [InlineData("ROLE_CHANGED", "ACCOUNT")]
    [InlineData("APPOINTMENT_BOOKED", "SCHEDULING")]
    [InlineData("SLOT_CREATED", "SCHEDULING")]
    [InlineData("DOCUMENT_UPLOADED", "CLINICAL")]
    [InlineData("PROFILE_VIEWED", "CLINICAL")]
    [InlineData("INTAKE_SUBMITTED", "CLINICAL")]
    [InlineData("AUDIT_LOG_VIEWED", "SECURITY")]
    [InlineData("AUDIT_LOG_EXPORTED", "SECURITY")]
    public void DeriveCategory_ReturnsExpected(string actionType, string expected)
    {
        Assert.Equal(expected, AuditLogReadRepository.DeriveCategory(actionType));
    }

    // ── PHI field masking ─────────────────────────────────────────────────────

    [Fact]
    public void MaskPhiFields_ReplacesKnownPhiFieldValues_WithRedacted()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            actorEmail = "admin@example.com",
            ipAddress = "10.0.0.1",
            patientId = "some-patient-guid",
            patientName = "Jane Doe",
            diagnosis = "Type 2 Diabetes",
            resource = "patient-profile",
        });

        var result = MaskAndDeserialize(metadata);

        Assert.Equal("[PHI-REDACTED]", result["patientId"].GetString());
        Assert.Equal("[PHI-REDACTED]", result["patientName"].GetString());
        Assert.Equal("[PHI-REDACTED]", result["diagnosis"].GetString());

        // Non-PHI fields must NOT be masked.
        Assert.Equal("admin@example.com", result["actorEmail"].GetString());
        Assert.Equal("10.0.0.1", result["ipAddress"].GetString());
        Assert.Equal("patient-profile", result["resource"].GetString());
    }

    [Fact]
    public void MaskPhiFields_WithNonPhiAction_DoesNotMask()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            actorEmail = "admin@example.com",
            patientId = "some-patient-guid",
        });

        // ExtractAndMaskMetadata only masks when phiAccess = true.
        var (_, _, _, _, _, _, maskedPayload) =
            AuditLogReadRepository.ExtractAndMaskMetadata(metadata, phiAccess: false);

        var result = JsonDocument.Parse(maskedPayload!).RootElement;

        // patientId must NOT be masked when phiAccess is false.
        Assert.Equal("some-patient-guid", result.GetProperty("patientId").GetString());
    }

    [Fact]
    public void MaskPhiFields_PhiAction_DoesNotMaskNonPhiFields()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            actorEmail = "admin@example.com",
            correlationId = "corr-001",
            patientName = "John Smith",
        });

        var (_, _, _, _, _, _, maskedPayload) =
            AuditLogReadRepository.ExtractAndMaskMetadata(metadata, phiAccess: true);

        var result = JsonDocument.Parse(maskedPayload!).RootElement;

        Assert.Equal("[PHI-REDACTED]", result.GetProperty("patientName").GetString());
        Assert.Equal("admin@example.com", result.GetProperty("actorEmail").GetString());
        Assert.Equal("corr-001", result.GetProperty("correlationId").GetString());
    }

    [Fact]
    public void ExtractAndMaskMetadata_ExtractsWellKnownFields()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            actorName = "Alice Admin",
            actorEmail = "alice@clinic.com",
            ipAddress = "192.168.1.1",
            userAgent = "Mozilla/5.0",
            correlationId = "c-001",
            sessionId = "s-001",
        });

        var (actorName, actorEmail, ipAddress, userAgent, correlationId, sessionId, _) =
            AuditLogReadRepository.ExtractAndMaskMetadata(metadata, phiAccess: false);

        Assert.Equal("Alice Admin", actorName);
        Assert.Equal("alice@clinic.com", actorEmail);
        Assert.Equal("192.168.1.1", ipAddress);
        Assert.Equal("Mozilla/5.0", userAgent);
        Assert.Equal("c-001", correlationId);
        Assert.Equal("s-001", sessionId);
    }

    [Fact]
    public void ExtractAndMaskMetadata_NullMetadata_ReturnsEmptyDefaults()
    {
        var (actorName, actorEmail, ipAddress, userAgent, correlationId, sessionId, maskedPayload) =
            AuditLogReadRepository.ExtractAndMaskMetadata(null, phiAccess: false);

        Assert.Equal(string.Empty, actorName);
        Assert.Equal(string.Empty, actorEmail);
        Assert.Null(ipAddress);
        Assert.Null(userAgent);
        Assert.Null(correlationId);
        Assert.Null(sessionId);
        Assert.Null(maskedPayload);
    }

    [Fact]
    public void MaskPhiFields_MalformedJson_ReturnsOriginalUnchanged()
    {
        const string badJson = "not-json-at-all";
        var result = AuditLogReadRepository.MaskPhiFields(badJson);
        Assert.Equal(badJson, result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, JsonElement> MaskAndDeserialize(string json)
    {
        var masked = AuditLogReadRepository.MaskPhiFields(json);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(masked)!;
    }
}
