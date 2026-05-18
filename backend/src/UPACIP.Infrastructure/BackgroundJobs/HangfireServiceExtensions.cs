using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Extension methods that wire Hangfire with PostgreSQL storage into the
/// ASP.NET Core DI container. Called from <see cref="DependencyInjection"/>.
/// </summary>
public static class HangfireServiceExtensions
{
    /// <summary>
    /// Registers Hangfire with PostgreSQL job store and the global exponential
    /// back-off retry filter. Connection string is read from the
    /// <c>ConnectionStrings:DefaultConnection</c> configuration key, which is
    /// expected to be supplied at runtime via the
    /// <c>ConnectionStrings__DefaultConnection</c> environment variable.
    /// </summary>
    public static IServiceCollection AddHangfireWithPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // In test/CI environments the connection string may be absent.
            // Log a warning and skip Hangfire setup so the build pipeline still works.
            services.AddLogging();
            using var sp = services.BuildServiceProvider();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("UPACIP.Infrastructure.Hangfire");
            logger.LogWarning(
                "Hangfire setup skipped — ConnectionStrings:DefaultConnection is not configured. " +
                "Set the ConnectionStrings__DefaultConnection environment variable in production.");
            return services;
        }

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                })
            .UseFilter(new ExponentialBackOffRetryFilter()));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(1, Environment.ProcessorCount);
            options.ServerName = $"UPACIP.API:{Environment.MachineName}";
        });

        return services;
    }

    /// <summary>
    /// Returns a <see cref="DashboardOptions"/> instance with Basic Auth
    /// using credentials supplied via environment variables:
    /// <list type="bullet">
    ///   <item><c>Hangfire__Dashboard__Username</c></item>
    ///   <item><c>Hangfire__Dashboard__Password</c></item>
    /// </list>
    /// </summary>
    public static DashboardOptions BuildDashboardOptions(IConfiguration configuration)
    {
        var username = configuration["Hangfire:Dashboard:Username"];
        var password = configuration["Hangfire:Dashboard:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "Hangfire dashboard credentials are not configured. " +
                "Set Hangfire__Dashboard__Username and Hangfire__Dashboard__Password " +
                "environment variables.");

        return new DashboardOptions
        {
            Authorization = [new BasicAuthDashboardFilter(username, password)],
            DashboardTitle = "UPACIP Background Jobs",
        };
    }
}
