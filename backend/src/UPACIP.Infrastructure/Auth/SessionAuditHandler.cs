using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Auth;

/// <summary>
/// Writes auth-pipeline audit events tied to session lifecycle boundaries.
/// </summary>
public static class SessionAuditHandler
{
    /// <summary>
    /// Emits a <c>SESSION_TIMEOUT</c> audit entry when an expired JWT is detected.
    /// Token parsing is best-effort and does not require signature validation.
    /// </summary>
    public static async Task WriteSessionTimeoutAuditAsync(
        AuthenticationFailedContext context,
        IAuditLogService auditLogService,
        CancellationToken cancellationToken = default)
    {
        var actorId = Guid.Empty;
        var actorRole = "Unknown";

        var token = ResolveToken(context);
        if (!string.IsNullOrWhiteSpace(token))
        {
            TryExtractActor(token, out actorId, out actorRole);
        }

        await auditLogService.LogAsync(
            actorId,
            actorRole,
            actionType: "SESSION_TIMEOUT",
            targetEntity: "Session",
            targetId: actorId,
            metadata: $"{{\"path\":\"{context.Request.Path}\",\"method\":\"{context.Request.Method}\"}}",
            cancellationToken: cancellationToken);
    }

    private static string? ResolveToken(AuthenticationFailedContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return context.Request.Cookies["__Host-access"];
    }

    private static void TryExtractActor(string token, out Guid actorId, out string actorRole)
    {
        actorId = Guid.Empty;
        actorRole = "Unknown";

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var parsedActorId))
            {
                actorId = parsedActorId;
            }

            var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (!string.IsNullOrWhiteSpace(role))
            {
                actorRole = role;
            }
        }
        catch
        {
            // Best-effort parsing only. Missing claims should not block the audit write.
        }
    }
}
