using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used exclusively by the <c>dotnet ef</c> CLI
/// to generate migrations without a running application host.
/// Reads <c>ConnectionStrings:DefaultConnection</c> from the API project's
/// appsettings files (environment-specific first, then base). Falls back to
/// a local placeholder so migration scaffolding works without a live database.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ResolveConnectionString())
            .Options;

        // The internal (design-time) constructor bypasses DI entirely.
        // Encryption is intentionally omitted — column types are text in both cases,
        // so migrations generated here are identical to production schema.
        return new AppDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // ASP.NET Core copies appsettings*.json to the output directory at build time.
        // AppContext.BaseDirectory points to the startup-project output (e.g. bin/Debug/net8.0/).
        // Directory.GetCurrentDirectory() points to wherever dotnet ef was invoked from.
        // Check both so the factory works regardless of invocation context.
        var searchRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "UPACIP.API"),
        };

        foreach (var root in searchRoots)
        {
            foreach (var file in new[] { $"appsettings.{env}.json", "appsettings.json" })
            {
                var conn = TryReadConnectionString(Path.Combine(root, file));
                if (conn is not null)
                    return conn;
            }
        }

        // Fallback: placeholder for migration generation without a live database.
        return "Host=localhost;Database=upacip_design;Username=postgres;Password=postgres";
    }

    private static string? TryReadConnectionString(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("DefaultConnection", out var conn) &&
                conn.ValueKind == JsonValueKind.String)
                return conn.GetString();
        }
        catch { /* unreadable file — skip */ }
        return null;
    }
}
