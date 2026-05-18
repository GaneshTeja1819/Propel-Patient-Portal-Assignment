using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Caching;

/// <summary>
/// Extension methods that wire Upstash Redis into the ASP.NET Core DI container.
///
/// Connection string is read exclusively from the <c>Redis:ConnectionString</c>
/// configuration key, supplied at runtime via the
/// <c>Redis__ConnectionString</c> environment variable (AC-005 — never committed).
///
/// Failure modes:
/// <list type="bullet">
///   <item>Missing connection string → warning logged; <see cref="ISlotCacheService"/>
///   registered as a null-safe no-op; API starts without crash (AC-002).</item>
///   <item>Unreachable server at runtime → <see cref="SlotCacheService"/> catches
///   <see cref="RedisException"/> and returns <see langword="null"/>; callers
///   fall through to database reads (AC-002 edge case).</item>
/// </list>
/// </summary>
public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Always register ISlotCacheService so consumers don't fail on startup.
            // The null multiplexer causes all cache calls to be silent no-ops.
            services.AddSingleton<ISlotCacheService>(sp =>
            {
                sp.GetRequiredService<ILogger<SlotCacheService>>().LogWarning(
                    "Redis:ConnectionString is not configured. " +
                    "Set the Redis__ConnectionString environment variable. " +
                    "Slot availability caching is disabled — reads will hit the database.");
                return new SlotCacheService(null, sp.GetRequiredService<ILogger<SlotCacheService>>());
            });
            return services;
        }

        // AbortOnConnectFail = false: Connect() returns immediately even if the
        // server is unreachable; StackExchange.Redis retries in the background.
        // Commands will throw RedisConnectionException until a connection is
        // established — SlotCacheService catches those and falls through.
        var options = ConfigurationOptions.Parse(connectionString);
        options.ConnectTimeout = 2_000;      // 2 s (AC-002)
        options.SyncTimeout = 2_000;
        options.AbortOnConnectFail = false;

        var multiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        // Use a factory so SlotCacheService receives null when the multiplexer
        // was not registered (e.g. an unexpected Connect exception above).
        services.AddSingleton<ISlotCacheService>(sp =>
            new SlotCacheService(
                sp.GetService<IConnectionMultiplexer>(),
                sp.GetRequiredService<ILogger<SlotCacheService>>()));

        return services;
    }
}
