using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using UPACIP.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

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

var app = builder.Build();

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
app.UseAuthorization();
app.MapControllers();

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
