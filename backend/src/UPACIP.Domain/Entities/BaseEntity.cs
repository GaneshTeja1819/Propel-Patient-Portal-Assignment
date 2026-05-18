namespace UPACIP.Domain.Entities;

/// <summary>
/// Base class for all domain entities. Provides a UUID primary key
/// assigned at construction time, preventing identity gaps from
/// database round-trips.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
}
