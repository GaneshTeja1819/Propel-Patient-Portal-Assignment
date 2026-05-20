using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UPACIP.Application.Commands.Conflicts;
using UPACIP.Application.Handlers.Conflicts;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Tests.UnitTests;

public sealed class MarkReviewedHandlerTests
{
    private readonly Mock<IConflictRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly MarkReviewedHandler _sut;

    private static readonly Guid ConflictId = Guid.NewGuid();
    private static readonly Guid ActorId    = Guid.NewGuid();
    private static readonly Guid PatientId  = Guid.NewGuid();

    public MarkReviewedHandlerTests()
    {
        _sut = new MarkReviewedHandler(
            _repoMock.Object,
            _uowMock.Object,
            _auditMock.Object,
            NullLogger<MarkReviewedHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_OpenConflict_SetsReviewedUnresolvedAndWritesAudit()
    {
        // Arrange
        var conflict = OpenConflict();
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);

        var command = new MarkReviewedCommand(ConflictId, ActorId);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        Assert.Equal("ReviewedUnresolved", conflict.Status);
        Assert.Equal(ActorId, conflict.ResolvedById);
        Assert.NotNull(conflict.ResolvedAt);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _auditMock.Verify(a => a.LogAsync(
            ActorId, "Staff", "CONFLICT_REVIEWED", "DataConflict", ConflictId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyReviewedUnresolved_IsIdempotentNoSave()
    {
        // Arrange — already in target state
        var conflict = OpenConflict();
        conflict.Status = "ReviewedUnresolved";
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);

        var command = new MarkReviewedCommand(ConflictId, ActorId);

        // Act
        await _sut.HandleAsync(command);

        // Assert — no DB write, no audit (idempotent)
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _auditMock.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyResolved_ThrowsInvalidOperationException()
    {
        // Arrange — resolved conflicts cannot be re-opened via mark-reviewed
        var conflict = OpenConflict();
        conflict.Status = "Resolved";
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync(conflict);

        var command = new MarkReviewedCommand(ConflictId, ActorId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(command));

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ConflictNotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(ConflictId, default)).ReturnsAsync((DataConflict?)null);
        var command = new MarkReviewedCommand(ConflictId, ActorId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    private static DataConflict OpenConflict() => new()
    {
        PatientId = PatientId,
        FieldName = "Medication Dosage",
        Status    = "Open",
        Severity  = "Medium",
    };
}
