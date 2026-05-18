using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using UPACIP.API.Filters;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure;
using UPACIP.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    // Map DbUpdateException (22001 column overflow) → HTTP 422 (AC-001 edge case).
    options.Filters.Add<DbUpdateExceptionFilter>();
});

// ── API Versioning ────────────────────────────────────────────────────────────
// URL segment versioning enforces /api/v1/ prefix on every route.
// AssumeDefaultVersionWhenUnspecified = false: requests to /api/health (no
// version segment) match no route → 404 (AC-003).
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Docs are populated by ConfigureSwaggerOptions below.
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

// ── Infrastructure (EF Core + Npgsql) ────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT Bearer Authentication (AC-001, AC-002) ────────────────────────────────
// Token is extracted from the __Host-access HttpOnly cookie, not the
// Authorization header, so the JWT is never accessible to browser JS.
// ClockSkew = Zero: token invalid at exactly T+15min (AC-002).
var jwtSigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
    ?? throw new InvalidOperationException(
        "JWT_SIGNING_KEY environment variable is not set. Cannot configure authentication.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Read JWT from HttpOnly cookie — never from Authorization header.
                context.Token = context.Request.Cookies["__Host-access"];
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Suppress default WWW-Authenticate details to avoid leaking
                // token structure or expiry information (AC-002, OWASP A01).
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
        };
    });

// ── CORS — InfinityFree origin only; no wildcard (AC-005, OWASP A05) ─────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("InfinityFreePolicy", policy =>
    {
        var allowedOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN");
        if (!string.IsNullOrWhiteSpace(allowedOrigin))
        {
            policy.WithOrigins(allowedOrigin)
                  .AllowCredentials()    // Required for cookie-based auth
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        // If ALLOWED_ORIGIN not set: no origins allowed — secure by default.
    });
});

// ── RBAC Policies (AC-001, AC-002) ────────────────────────────────────────────
// Each policy enforces a single allowed role value from the `role` JWT claim.
// RoleClaimType = "role" is set on the JWT bearer, so RequireRole resolves
// against the short claim name.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PatientPolicy", policy => policy.RequireRole("Patient"));
    options.AddPolicy("StaffPolicy",   policy => policy.RequireRole("Staff"));
    options.AddPolicy("AdminPolicy",   policy => policy.RequireRole("Admin"));
});

// Log the role claim at Warning level when authorization fails with HTTP 403
// so that cross-role access attempts are visible in audit logs (AC-001).
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, RbacLoggingResultHandler>();

var app = builder.Build();

// ── Encryption key validation (AC-002) ───────────────────────────────────────
// Eagerly resolve the singleton so an invalid/missing PHI_ENCRYPTION_KEY throws
// at startup rather than at first request.
app.Services.GetRequiredService<IEncryptionService>();

// ── Gemini API key validation ─────────────────────────────────────────────────
// GeminiClient constructor throws InvalidOperationException if GEMINI_API_KEY
// is absent — eager resolve here ensures the process exits non-zero at startup.
app.Services.GetRequiredService<IGeminiClient>();

// ── Redis startup PING (AC-002) ───────────────────────────────────────────────
var redisMultiplexer = app.Services.GetService<IConnectionMultiplexer>();
if (redisMultiplexer is not null)
{
    try
    {
        var pong = await redisMultiplexer.GetDatabase()
            .ExecuteAsync("PING")
            .WaitAsync(TimeSpan.FromSeconds(2));
        app.Logger.LogInformation("Redis PING: {Pong} — connection healthy.", pong);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Redis unavailable — falling back to database reads.");
    }
}

// ── Swagger UI (development only — AC-004) ────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"UPACIP API {description.GroupName.ToUpperInvariant()}");
        }
    });
}

app.UseHttpsRedirection();
app.UseCors("InfinityFreePolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Hangfire Dashboard (all environments — Basic Auth required) ───────────────
app.UseHangfireDashboard("/hangfire", HangfireServiceExtensions.BuildDashboardOptions(builder.Configuration));

app.Run();

// ── Swagger options helper ────────────────────────────────────────────────────
/// <summary>
/// Registers one SwaggerDoc per discovered API version so that Swagger UI
/// lists version-specific endpoints (AC-004).
/// </summary>
internal sealed class ConfigureSwaggerOptions
    : Microsoft.Extensions.Options.IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        => _provider = provider;

    public void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "UPACIP API",
                Version = description.GroupName,
                Description = description.IsDeprecated
                    ? "This API version has been deprecated."
                    : "Unified Patient Access & Clinical Intelligence Platform REST API."
            });
        }
    }
}

// ── RBAC logging handler ──────────────────────────────────────────────────────
/// <summary>
/// Intercepts authorization failures so the caller's <c>role</c> JWT claim can
/// be logged at <see cref="LogLevel.Warning"/> before the 403 response is sent.
/// This satisfies AC-001: cross-role access attempts are visible in audit logs
/// without leaking other JWT claim values.
/// </summary>
internal sealed class RbacLoggingResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();
    private readonly ILogger<RbacLoggingResultHandler> _logger;

    public RbacLoggingResultHandler(ILogger<RbacLoggingResultHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // Log role claim only on Forbidden (authenticated but wrong role — AC-001).
        // Do NOT log on Challenge (unauthenticated) to avoid noise on public probes.
        if (authorizeResult.Forbidden)
        {
            var role = context.User.FindFirstValue("role") ?? "(missing)";
            _logger.LogWarning(
                "Authorization denied for request {Method} {Path}: role={Role} does not satisfy policy.",
                context.Request.Method,
                context.Request.Path,
                role);
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
