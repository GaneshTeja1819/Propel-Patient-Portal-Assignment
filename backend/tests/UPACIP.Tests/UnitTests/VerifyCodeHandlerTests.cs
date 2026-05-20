using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UPACIP.Application.Commands.Codes;
using UPACIP.Application.Handlers.Codes;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;
using UPACIP.Infrastructure.Handlers.Codes;
using UPACIP.Infrastructure.Persistence;
using UPACIP.Infrastructure.Reference;

namespace UPACIP.Tests.UnitTests;

/// <summary>
/// Unit tests for <see cref="VerifyCodeHandler"/> (US_030, AC-001–AC-005).
/// Uses EF Core InMemory provider to avoid PostgreSQL dependency.
/// </summary>
public sealed class VerifyCodeHandlerTests : IDisposable
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly IIcdCptReferenceService _referenceService = new IcdCptReferenceService();
    private readonly VerifyCodeHandler _sut;

    private static readonly Guid StaffId = Guid.NewGuid();

    public VerifyCodeHandlerTests()
    {
        // Use InMemory provider per test instance (unique name prevents cross-test pollution).
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Use the internal design-time constructor (no encryption needed for unit tests).
        _db = (AppDbContext)Activator.CreateInstance(
            typeof(AppDbContext),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object[] { options },
            null)!;

        // SaveChangesAsync delegates to the real EF InMemory save.
        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _db.SaveChangesAsync(ct));

        _sut = new VerifyCodeHandler(
            _db,
            _uowMock.Object,
            _auditMock.Object,
            _referenceService,
            NullLogger<VerifyCodeHandler>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds one ExtractedClinicalData + one MedicalCodeSuggestion.
    /// Returns their auto-assigned IDs (BaseEntity.Id is private set).
    /// </summary>
    private async Task<(Guid suggestionId, Guid clinicalDataId)> SeedSuggestion(
        string codeSystem = "ICD10",
        string codingStatus = "Pending")
    {
        var clinicalData = new ExtractedClinicalData
        {
            EncryptedExtractedJson = "{}",
            ExtractionModel        = "gemini",
            CodingStatus           = codingStatus,
        };
        _db.ExtractedClinicalData.Add(clinicalData);
        await _db.SaveChangesAsync();

        var suggestion = new MedicalCodeSuggestion
        {
            ClinicalDataId  = clinicalData.Id,
            PatientId       = Guid.NewGuid(),
            CodeSystem      = codeSystem,
            SuggestedCode   = "J18.9",
            Description     = "Pneumonia",
            ConfidenceScore = 0.92,
            Rank            = 1,
            Status          = "Pending",
            ModelVersion    = "v1",
            PromptHash      = "abc123",
        };
        _db.MedicalCodeSuggestions.Add(suggestion);
        await _db.SaveChangesAsync();

        return (suggestion.Id, clinicalData.Id);
    }

    // ── AC-002: Accepted path ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Accepted_CreatesVerifiedCodeWithSuggestedCode()
    {
        var (suggId, _) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "Accepted", null, StaffId);

        var result = await _sut.HandleAsync(command);

        var saved = await _db.VerifiedMedicalCodes.FindAsync(result.VerifiedMedicalCodeId);
        Assert.NotNull(saved);
        Assert.Equal("Accepted", saved.Decision);
        Assert.Equal("J18.9", saved.Code);
        Assert.Null(saved.OriginalSuggestedCode);
        Assert.Equal(StaffId, saved.VerifiedById);
    }

    [Fact]
    public async Task HandleAsync_Accepted_SetsCodingStatusComplete()
    {
        var (suggId, cdId) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "Accepted", null, StaffId);

        var result = await _sut.HandleAsync(command);

        var cd = await _db.ExtractedClinicalData.FindAsync(cdId);
        Assert.Equal("Complete", cd!.CodingStatus);
        Assert.True(result.CodingStatusUpdated);
    }

    [Fact]
    public async Task HandleAsync_Accepted_WritesCodeVerifiedAudit()
    {
        var (suggId, _) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "Accepted", null, StaffId);

        await _sut.HandleAsync(command);

        _auditMock.Verify(a => a.LogAsync(
            StaffId, "Staff", "CODE_VERIFIED", "VerifiedMedicalCode",
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── AC-003: Modified path ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Modified_ValidCode_CreatesRecordWithVerifiedCode()
    {
        var (suggId, _) = await SeedSuggestion(codeSystem: "ICD10");
        var command = new VerifyCodeCommand(suggId, "Modified", "Z00.00", StaffId);

        var result = await _sut.HandleAsync(command);

        var saved = await _db.VerifiedMedicalCodes.FindAsync(result.VerifiedMedicalCodeId);
        Assert.NotNull(saved);
        Assert.Equal("Modified", saved.Decision);
        Assert.Equal("Z00.00", saved.Code);
        Assert.Equal("J18.9", saved.OriginalSuggestedCode); // original suggested code preserved
    }

    [Fact]
    public async Task HandleAsync_Modified_InvalidCode_ThrowsUnprocessable()
    {
        var (suggId, _) = await SeedSuggestion(codeSystem: "ICD10");
        var command = new VerifyCodeCommand(suggId, "Modified", "INVALID!!", StaffId);

        await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => _sut.HandleAsync(command));

        // No record should have been created.
        Assert.Empty(await _db.VerifiedMedicalCodes.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_Modified_ValidIcd10_7thCharExtension_Passes()
    {
        // Validates GAP-002 fix: S72.001A is a valid 7th-character ICD-10-CM code.
        var (suggId, _) = await SeedSuggestion(codeSystem: "ICD10");
        var command = new VerifyCodeCommand(suggId, "Modified", "S72.001A", StaffId);

        var result = await _sut.HandleAsync(command);

        var saved = await _db.VerifiedMedicalCodes.FindAsync(result.VerifiedMedicalCodeId);
        Assert.Equal("S72.001A", saved!.Code);
    }

    // ── AC-004: Rejected path ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Rejected_CreatesRecordWithEmptyCode()
    {
        var (suggId, _) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "Rejected", null, StaffId);

        var result = await _sut.HandleAsync(command);

        var saved = await _db.VerifiedMedicalCodes.FindAsync(result.VerifiedMedicalCodeId);
        Assert.NotNull(saved);
        Assert.Equal("Rejected", saved.Decision);
        Assert.Equal(string.Empty, saved.Code);
    }

    // ── AC-005: All-rejected path ─────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_AllRejected_SetsCodingStatusPendingManualCoding()
    {
        // Single suggestion — rejecting it triggers all-rejected condition.
        var (suggId, cdId) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "Rejected", null, StaffId);

        var result = await _sut.HandleAsync(command);

        var cd = await _db.ExtractedClinicalData.FindAsync(cdId);
        Assert.Equal("PendingManualCoding", cd!.CodingStatus);
        Assert.True(result.CodingStatusUpdated);
    }

    [Fact]
    public async Task HandleAsync_AllRejected_WritesMassRejectionAudit()
    {
        var (suggId, cdId) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "Rejected", null, StaffId);

        await _sut.HandleAsync(command);

        _auditMock.Verify(a => a.LogAsync(
            StaffId, "Staff", "MASS_REJECTION", "ExtractedClinicalData",
            cdId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Edge case: Finalized encounter ────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_FinalizedEncounter_ThrowsUnprocessable()
    {
        var (suggId, _) = await SeedSuggestion(codingStatus: "Complete");
        var command = new VerifyCodeCommand(suggId, "Accepted", null, StaffId);

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => _sut.HandleAsync(command));

        Assert.Contains("finalised", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Edge case: Duplicate verification ────────────────────────────────────

    [Fact]
    public async Task HandleAsync_DuplicateVerification_ThrowsConflict()
    {
        var (suggId, _) = await SeedSuggestion();
        // First verification succeeds.
        await _sut.HandleAsync(new VerifyCodeCommand(suggId, "Accepted", null, StaffId));

        // Second attempt on the same suggestion — application-level guard fires first.
        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.HandleAsync(
                new VerifyCodeCommand(suggId, "Rejected", null, StaffId)));
    }

    // ── Edge case: Defensive Decision guard ───────────────────────────────────

    [Fact]
    public async Task HandleAsync_InvalidDecision_ThrowsUnprocessable()
    {
        var (suggId, _) = await SeedSuggestion();
        var command = new VerifyCodeCommand(suggId, "UNKNOWN", null, StaffId);

        await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => _sut.HandleAsync(command));
    }
}
