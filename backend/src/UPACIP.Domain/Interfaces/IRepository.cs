using UPACIP.Domain.Entities;

namespace UPACIP.Domain.Interfaces;

/// <summary>
/// Generic repository contract. Implementations live in Infrastructure;
/// callers depend only on this abstraction in Domain/Application.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
