using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using UPACIP.Application.Handlers.Admin;
using UPACIP.Application.Handlers.Auth;
using UPACIP.Application.Handlers.Codes;
using UPACIP.Application.Handlers.Documents;
using UPACIP.Application.Handlers.Intake;
using UPACIP.Application.Handlers.Conflicts;
using UPACIP.Infrastructure.Handlers.Codes;
using UPACIP.Infrastructure.Handlers.Profile;
using UPACIP.Infrastructure.Reference;
using UPACIP.Application.Handlers.Appointments;
using UPACIP.Application.Handlers.Slots;
using UPACIP.Application.Handlers.Appointments;
using UPACIP.Application.Handlers.Patients;
using UPACIP.Application.Handlers.Queue;
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
using UPACIP.Infrastructure.Repositories;
using UPACIP.Infrastructure.Persistence.QueryServices;
using UPACIP.Infrastructure.Security;
using UPACIP.Infrastructure.Services;

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
        services.AddScoped<IAdminUserStore, AdminUserStore>();
        services.AddScoped<ISlotQueryService, SlotQueryService>();
        services.AddScoped<IAppointmentBookingStore, BookingStore>();
        services.AddScoped<IAppointmentManagementStore, AppointmentManagementStore>();
        services.AddScoped<IWalkInBookingStore, WalkInBookingStore>();
        services.AddScoped<IQueueStore, QueueStore>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<ChangeRoleHandler>();
        services.AddScoped<DeactivateUserHandler>();

        // ── Audit log read (admin_read_role / audit_reader credentials) ────
        // Falls back to DefaultConnection in development; in production the
        // AuditReadConnection key uses credentials for the audit_reader role.
        var auditReadCs = configuration.GetConnectionString("AuditReadConnection")
            ?? connectionString
            ?? throw new InvalidOperationException(
                "Neither AuditReadConnection nor DefaultConnection is configured.");
        services.AddSingleton(new AuditReadDbContext(auditReadCs));
        services.AddScoped<IAuditLogReadRepository, AuditLogReadRepository>();
        services.AddScoped<GetAuditLogHandler>();
        services.AddScoped<GetAuditLogStatsHandler>();

        // ── Intake confirm (US_018, task_003) ──────────────────────────────
        services.AddScoped<IIntakeRecordRepository, IntakeRecordRepository>();
        services.AddScoped<ConfirmIntakeHandler>();

        // ── Conflict management (US_028, task_002) ─────────────────────────
        services.AddScoped<IConflictRepository, ConflictRepository>();
        services.AddScoped<ResolveConflictHandler>();
        services.AddScoped<MarkReviewedHandler>();
        services.AddScoped<GetSlotsHandler>();
        services.AddScoped<BookAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<RescheduleAppointmentHandler>();
        services.AddSingleton<INoShowRiskScorer, NoShowRiskScorer>();
        services.AddScoped<WalkInBookingHandler>();
        services.AddScoped<CreatePatientFromWalkInHandler>();
        services.AddScoped<GetTodaysQueueHandler>();
        services.AddScoped<ArriveQueueEntryHandler>();
        services.AddScoped<ReorderQueueEntryHandler>();
        services.AddScoped<RemoveQueueEntryHandler>();

        services.AddHangfireWithPostgres(configuration);
        services.AddTransient<AccountLockoutNotificationJob>();
        services.AddTransient<GeneratePdfConfirmationJob>();
        services.AddTransient<AppointmentReminderJob>();
        services.AddTransient<WaitlistNotificationJob>();
        services.AddTransient<SlotSwapJob>();
        services.AddTransient<SlotSwapNotificationJob>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        // US_020: SMS gateway — HttpClient managed to avoid socket exhaustion
        services.AddHttpClient<ISmsService, SmsService>();
        services.AddScoped<IAccountLockoutNotifier, HangfireAccountLockoutNotifier>();
        services.AddScoped<IPdfConfirmationJobEnqueuer, HangfirePdfConfirmationJobEnqueuer>();
        services.AddScoped<ISlotSwapJobEnqueuer, HangfireSlotSwapJobEnqueuer>();
        services.AddScoped<IWaitlistNotificationJobEnqueuer, HangfireWaitlistNotificationJobEnqueuer>();
        services.AddScoped<IReminderJobEnqueuer, HangfireReminderJobEnqueuer>();
        // US_021: Calendar sync — HttpClient managed to avoid socket exhaustion
        services.AddHttpClient<ICalendarSyncService, CalendarSyncService>();
        services.AddRedis(configuration);

        // ── Auth (JWT + Redis sessions) ──────────────────────────────────
        // RedisSessionStore takes IConnectionMultiplexer? (nullable) — resolves to
        // null when Redis:ConnectionString is absent; operations throw at call-time.
        services.AddScoped<RedisSessionStore>(sp => new RedisSessionStore(
            sp.GetService<IConnectionMultiplexer>(),
            sp.GetRequiredService<ILogger<RedisSessionStore>>()));

        // JwtAuthService constructor validates JWT_SIGNING_KEY at startup.
        services.AddScoped<IAuthService, JwtAuthService>();

        // ── Documents (US_025) ────────────────────────────────────────────────
        services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();

        // SupabaseStorageService reads env vars at construction time and validates
        // them eagerly, surfacing misconfiguration at startup (OWASP A02).
        // AddHttpClient registers a typed client; SupabaseStorageService receives
        // a managed HttpClient instance, avoiding socket exhaustion.
        services.AddHttpClient<IDocumentStorageService, SupabaseStorageService>();

        services.AddScoped<IClinicalDocumentRepository, ClinicalDocumentRepository>();
        services.AddScoped<UploadDocumentHandler>();
        services.AddScoped<GetPatientProfileHandler>();

        // Hangfire dispatcher: bridges Application's IDocumentExtractionJobDispatcher
        // to Hangfire's IBackgroundJobClient without coupling Application to Hangfire.
        services.AddScoped<IDocumentExtractionJobDispatcher, HangfireDocumentExtractionJobDispatcher>();

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

        // ── AI Intake Session (US_018) ─────────────────────────────────────
        // IntakeQuestions.json is embedded in the Infrastructure assembly and
        // loaded once at startup; the resulting array is shared across all scopes.
        services.AddSingleton<IReadOnlyList<IntakeQuestion>>(sp =>
        {
            var assembly = typeof(DependencyInjection).Assembly;
            const string ResourceName = "UPACIP.Infrastructure.AI.IntakeQuestions.json";
            using var stream = assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{ResourceName}' not found in UPACIP.Infrastructure assembly. " +
                    "Ensure the file Build Action is set to 'Embedded Resource'.");

            var questions = JsonSerializer.Deserialize<IntakeQuestion[]>(
                stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("IntakeQuestions.json deserialized to null.");

            return questions;
        });

        // GeminiIntakeAdapter is scoped — it depends on the singleton IGeminiClient.
        services.AddScoped<GeminiIntakeAdapter>();

        // GeminiExtractionAdapter + deduplication adapters/jobs are scoped so each
        // Hangfire job execution gets its own EF Core DbContext scope (US_026, US_027).
        services.AddScoped<GeminiExtractionAdapter>();
        services.AddScoped<GeminiDeduplicationAdapter>();
        services.AddScoped<ClinicalDataExtractionJob>();
        services.AddScoped<DeduplicationJob>();

        // Code suggestion adapter + job (US_029, AC-001).
        // GeminiCodingAdapter is scoped so PromptHash is stable within a single job execution.
        services.AddScoped<GeminiCodingAdapter>();
        services.AddScoped<CodeSuggestionJob>();

        // Code verification (US_030, AC-001–AC-005).
        // IcdCptReferenceService is singleton: compiled regex patterns are shared
        // safely across all requests; no mutable state.
        services.AddSingleton<IIcdCptReferenceService, IcdCptReferenceService>();
        services.AddScoped<VerifyCodeHandler>();

        // IntakeSessionService is scoped — it takes IConnectionMultiplexer? (nullable).
        services.AddScoped<IIntakeSessionService>(sp => new IntakeSessionService(
            sp.GetService<IConnectionMultiplexer>(),
            sp.GetRequiredService<GeminiIntakeAdapter>(),
            sp.GetRequiredService<IReadOnlyList<IntakeQuestion>>(),
            sp.GetRequiredService<IEncryptionService>(),
            sp.GetRequiredService<ILogger<IntakeSessionService>>()));

        return services;
    }
}
