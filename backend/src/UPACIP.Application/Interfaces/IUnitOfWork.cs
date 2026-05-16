namespace UPACIP.Application.Interfaces;

/// <summary>
/// Abstracts the persistence commit boundary. Infrastructure
/// implements this; Application orchestrates it without taking
/// a hard dependency on EF Core.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
