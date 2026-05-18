namespace UPACIP.Application.Interfaces;

/// <summary>
/// Transaction boundary abstraction used by Application workflows.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
