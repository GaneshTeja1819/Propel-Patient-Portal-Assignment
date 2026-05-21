using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UPACIP.Application.Commands.Conflicts;
using UPACIP.Application.Handlers.Conflicts;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Tests.UnitTests;

public sealed class ResolveConflictHandlerTests
{
    private readonly Mock<IConflictRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly ResolveConflictHandler _sut;

    private static readonly Guid ConflictId = Guid.NewGuid();
    private static readonly Guid ActorId    = Guid.NewGuid();
    private static readonly Guid PatientId  = Guid.NewGuid();

    public ResolveConflictHandlerTests()
    {
        _sut = new ResolveConflictHandler(
            _repoMock.Object,
            _uowMock.Object,
            _auditMock.Object,
            NullLogger<ResolveConflictHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_OpenConflict_SetsResolvedAndWritesAudit()
    {
        // Arrange
        var conflict = OpenConflict();
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);

        var command = new ResolveConflictCommand(ConflictId, ActorId, "Metoprolol 50 mg", null, null);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        Assert.Equal("Resolved", conflict.Status);
        Assert.Equal("Metoprolol 50 mg", conflict.CanonicalValue);
        Assert.Equal(ActorId, conflict.ResolvedById);
        Assert.NotNull(conflict.ResolvedAt);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _auditMock.Verify(a => a.LogAsync(
            ActorId, "Staff", "CONFLICT_RESOLVED", "DataConflict", ConflictId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Resolved")]
    [InlineData("ReviewedUnresolved")]
    public async Task HandleAsync_NonOpenStatus_ThrowsInvalidOperationException(string status)
    {
        // Arrange
        var conflict = OpenConflict();
        conflict.Status = status;
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);

        var command = new ResolveConflictCommand(ConflictId, ActorId, "some value", null, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(command));

        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ConflictNotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync((DataConflict?)null);
        var command = new ResolveConflictCommand(ConflictId, ActorId, "value", null, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyAuthoritativeValue_ThrowsArgumentException(string badValue)
    {
        var conflict = OpenConflict();
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);
        var command = new ResolveConflictCommand(ConflictId, ActorId, badValue, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.HandleAsync(command));
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TrimsAuthoritativeValue()
    {
        var conflict = OpenConflict();
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);
        var command = new ResolveConflictCommand(ConflictId, ActorId, "  value  ", null, null);

        await _sut.HandleAsync(command);

        Assert.Equal("value", conflict.CanonicalValue);
    }

    private static DataConflict OpenConflict() => new()
    {
        PatientId = PatientId,
        FieldName = "Medication Dosage",
        Status    = "Open",
        Severity  = "High",
    };
}
