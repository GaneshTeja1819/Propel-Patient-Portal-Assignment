using Microsoft.EntityFrameworkCore;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the UPACIP platform.
/// Connection string is injected at startup from environment variables
/// (ConnectionStrings__DefaultConnection) — never hardcoded here.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
