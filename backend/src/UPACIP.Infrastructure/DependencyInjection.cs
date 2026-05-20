using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UPACIP.Application.Handlers.Auth;
using UPACIP.Application.Handlers.Appointments;
using UPACIP.Application.Handlers.Slots;
using UPACIP.Application.Interfaces;
using UPACIP.Application.Services;
using UPACIP.Infrastructure.AI;
using UPACIP.Infrastructure.Audit;
using UPACIP.Infrastructure.Auth;
using UPACIP.Infrastructure.BackgroundJobs;
using UPACIP.Infrastructure.Caching;
using UPACIP.Infrastructure.Documents;
using UPACIP.Infrastructure.Persistence;
using UPACIP.Infrastructure.Persistence.Interceptors;
using UPACIP.Infrastructure.Persistence.QueryServices;
using UPACIP.Infrastructure.Security;

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

        // ── Security (PHI encryption) ────────────────────────────────────
        // Singleton: key is loaded once at startup; constructor throws if
        // PHI_ENCRYPTION_KEY is absent or not 32 decoded bytes (AC-002).
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // ── Audit ────────────────────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        // ── EF Core ──────────────────────────────────────────────────────
        // The (sp, options) overload lets us resolve scoped interceptor per request.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRegistrationStore, RegistrationStore>();
        services.AddScoped<ILoginUserStore, LoginUserStore>();
        services.AddScoped<ISlotQueryService, SlotQueryService>();
        services.AddScoped<IAppointmentBookingStore, BookingStore>();
        services.AddScoped<IAppointmentManagementStore, AppointmentManagementStore>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<GetSlotsHandler>();
        services.AddScoped<BookAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<RescheduleAppointmentHandler>();
        services.AddSingleton<INoShowRiskScorer, NoShowRiskScorer>();

        services.AddHangfireWithPostgres(configuration);
        services.AddTransient<AccountLockoutNotificationJob>();
        services.AddTransient<GeneratePdfConfirmationJob>();
        services.AddTransient<WaitlistNotificationJob>();
        services.AddTransient<SlotSwapJob>();
        services.AddTransient<SlotSwapNotificationJob>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IAccountLockoutNotifier, HangfireAccountLockoutNotifier>();
        services.AddScoped<IPdfConfirmationJobEnqueuer, HangfirePdfConfirmationJobEnqueuer>();
        services.AddScoped<ISlotSwapJobEnqueuer, HangfireSlotSwapJobEnqueuer>();
        services.AddScoped<IWaitlistNotificationJobEnqueuer, HangfireWaitlistNotificationJobEnqueuer>();
        services.AddRedis(configuration);

        // ── Auth (JWT + Redis sessions) ──────────────────────────────────
        // RedisSessionStore takes IConnectionMultiplexer? (nullable) — resolves to
        // null when Redis:ConnectionString is absent; operations throw at call-time.
        services.AddScoped<RedisSessionStore>(sp => new RedisSessionStore(
            sp.GetService<IConnectionMultiplexer>(),
            sp.GetRequiredService<ILogger<RedisSessionStore>>()));

        // JwtAuthService constructor validates JWT_SIGNING_KEY at startup.
        services.AddScoped<IAuthService, JwtAuthService>();

        // ── Documents ─────────────────────────────────────────────────────────
        services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();

        // ── AI (Gemini) ─────────────────────────────────────────────────────
        // GeminiClient validates GEMINI_API_KEY at construction; singleton is safe
        // because the SDK GenerativeModel is stateless and thread-safe.
        // GeminiInvocationLogger wraps it as IGeminiClient and uses IServiceScopeFactory
        // to create short-lived scopes for each IAuditLogService (scoped) write.
        services.AddSingleton<GeminiClient>(sp => new GeminiClient(
            sp.GetRequiredService<ILogger<GeminiClient>>()));
        services.AddSingleton<IGeminiClient>(sp => new GeminiInvocationLogger(
            sp.GetRequiredService<GeminiClient>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<GeminiInvocationLogger>>()));

        return services;
    }
}
