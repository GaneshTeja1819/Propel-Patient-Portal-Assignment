using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.BackgroundJobs;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure;

/// <summary>
/// Registers all Infrastructure services with the DI container.
/// Called from API layer's Program.cs — keeps Infrastructure
/// wiring internal to this assembly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHangfireWithPostgres(configuration);

        return services;
    }
}
