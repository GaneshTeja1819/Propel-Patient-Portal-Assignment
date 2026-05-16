using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace UPACIP.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire dashboard authorization filter enforcing HTTP Basic Authentication.
///
/// Credentials are injected at construction time — they MUST be loaded from
/// environment variables by the caller (never from appsettings values directly).
///
/// Security notes:
/// - Credential comparison uses <see cref="CryptographicOperations.FixedTimeEquals"/>
///   to prevent timing-oracle attacks.
/// - Returns HTTP 401 + WWW-Authenticate header so browsers prompt for credentials.
/// - HTTPS is assumed; Basic Auth over plain HTTP is insecure (TR-005).
/// </summary>
internal sealed class BasicAuthDashboardFilter : IDashboardAuthorizationFilter
{
    private readonly byte[] _usernameBytes;
    private readonly byte[] _passwordBytes;

    public BasicAuthDashboardFilter(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        _usernameBytes = Encoding.UTF8.GetBytes(username);
        _passwordBytes = Encoding.UTF8.GetBytes(password);
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Challenge(httpContext);
            return false;
        }

        string encoded = authHeader["Basic ".Length..].Trim();
        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            Challenge(httpContext);
            return false;
        }

        int colonIndex = decoded.IndexOf(':');
        if (colonIndex <= 0)
        {
            Challenge(httpContext);
            return false;
        }

        var suppliedUsername = Encoding.UTF8.GetBytes(decoded[..colonIndex]);
        var suppliedPassword = Encoding.UTF8.GetBytes(decoded[(colonIndex + 1)..]);

        bool valid =
            CryptographicOperations.FixedTimeEquals(suppliedUsername, _usernameBytes) &&
            CryptographicOperations.FixedTimeEquals(suppliedPassword, _passwordBytes);

        if (!valid)
            Challenge(httpContext);

        return valid;
    }

    private static void Challenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
    }
}
