using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUnitOfWork. Delegates directly to
/// AppDbContext.SaveChangesAsync so Application layer stays
/// decoupled from EF Core.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}
