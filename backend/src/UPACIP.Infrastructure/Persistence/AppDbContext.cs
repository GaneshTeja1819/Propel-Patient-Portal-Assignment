using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;
using UPACIP.Infrastructure.Persistence.Converters;
using UPACIP.Infrastructure.Persistence.Seeders;

namespace UPACIP.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the UPACIP platform.
/// Connection string is injected at startup from environment variables
/// (ConnectionStrings__DefaultConnection) — never hardcoded here.
/// All FK relationships use DeleteBehavior.Restrict to prevent accidental
/// cascade deletes on PHI data (HIPAA alignment).
/// PHI columns are encrypted at rest via <see cref="EncryptedStringConverter"/> (AC-001).
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly IEncryptionService? _encryptionService;

    /// <summary>Runtime constructor — DI injects <paramref name="encryptionService"/>.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService encryptionService)
        : base(options)
    {
        _encryptionService = encryptionService;
    }

    /// <summary>Design-time / migration constructor — no encryption (schema generation only).</summary>
    internal AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<IntakeRecord> IntakeRecords => Set<IntakeRecord>();
    public DbSet<ClinicalDocument> ClinicalDocuments => Set<ClinicalDocument>();
    public DbSet<ExtractedClinicalData> ExtractedClinicalData => Set<ExtractedClinicalData>();
    public DbSet<PatientProfile360> PatientProfiles360 => Set<PatientProfile360>();
    public DbSet<DataConflict> DataConflicts => Set<DataConflict>();
    public DbSet<MedicalCodeSuggestion> MedicalCodeSuggestions => Set<MedicalCodeSuggestion>();
    public DbSet<MergedClinicalEntry> MergedClinicalEntries => Set<MergedClinicalEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<InsuranceRecord> InsuranceRecords => Set<InsuranceRecord>();
    public DbSet<CalendarSync> CalendarSyncs => Set<CalendarSync>();
    public DbSet<VerifiedMedicalCode> VerifiedMedicalCodes => Set<VerifiedMedicalCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // pgvector extension — required for future embedding columns
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── User ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(u => u.Role).HasMaxLength(50).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PhoneNumber).HasMaxLength(30);
            e.Property(u => u.LastConflictReviewedAt).IsRequired(false);
            e.Property(u => u.FailedLoginCount).HasDefaultValue(0);
            e.Property(u => u.LockUntil);
        });

        // ── AppointmentSlot ───────────────────────────────────────────────
        modelBuilder.Entity<AppointmentSlot>(e =>
        {
            e.ToTable("appointment_slots");
            e.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
            e.HasOne(s => s.Provider)
             .WithMany(u => u.AppointmentSlots)
             .HasForeignKey(s => s.ProviderId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Appointment ───────────────────────────────────────────────────
        modelBuilder.Entity<Appointment>(e =>
        {
            e.ToTable("appointments");
            e.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
            e.Property(a => a.CreatedByStaffId);
            e.Property(a => a.AnonymousPatientDetails).HasColumnType("jsonb");
            e.HasOne(a => a.Patient)
             .WithMany(u => u.PatientAppointments)
             .HasForeignKey(a => a.PatientId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Provider)
             .WithMany(u => u.ProviderAppointments)
             .HasForeignKey(a => a.ProviderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Slot)
             .WithMany(s => s.Appointments)
             .HasForeignKey(a => a.SlotId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PHI value converter — shared across all encrypted columns (AC-001) ──
        EncryptedStringConverter? phiConverter = _encryptionService is not null
            ? new EncryptedStringConverter(_encryptionService)
            : null;

        // ── IntakeRecord ──────────────────────────────────────────────
        modelBuilder.Entity<IntakeRecord>(e =>
        {
            e.ToTable("intake_records");
            e.Property(r => r.EncryptedFormData).HasColumnType("text");
            if (phiConverter is not null)
                e.Property(r => r.EncryptedFormData).HasConversion(phiConverter);
            e.HasOne(r => r.Patient)
             .WithMany()
             .HasForeignKey(r => r.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Appointment)
             .WithMany(a => a.IntakeRecords)
             .HasForeignKey(r => r.AppointmentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ClinicalDocument ──────────────────────────────────────────────
        modelBuilder.Entity<ClinicalDocument>(e =>
        {
            e.ToTable("clinical_documents");
            // StoragePath is encrypted at rest (AC-005, DR-005) — EF Core converter applies
            // AES-256-GCM transparently on SaveChanges. FileHash is stored plaintext and indexed.
            if (phiConverter is not null)
                e.Property(d => d.StoragePath).HasConversion(phiConverter);
            e.Property(d => d.ExtractionStatus).HasMaxLength(20).IsRequired();
            e.Property(d => d.FileHash).HasMaxLength(64).IsRequired();
            // Composite index enables efficient per-patient duplicate detection (SHA-256 dedup).
            e.HasIndex(d => new { d.PatientId, d.FileHash }).IsUnique();
            e.HasOne(d => d.Patient)
             .WithMany()
             .HasForeignKey(d => d.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Provider)
             .WithMany()
             .HasForeignKey(d => d.ProviderId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Appointment)
             .WithMany(a => a.ClinicalDocuments)
             .HasForeignKey(d => d.AppointmentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ExtractedClinicalData ─────────────────────────────────────────
        modelBuilder.Entity<ExtractedClinicalData>(e =>
        {
            e.ToTable("extracted_clinical_data");
            e.Property(x => x.CodingStatus)
             .HasColumnName("coding_status")
             .HasMaxLength(32)
             .HasDefaultValue("Pending")
             .IsRequired();
            e.Property(x => x.EncryptedExtractedJson).HasColumnType("text");
            if (phiConverter is not null)
                e.Property(x => x.EncryptedExtractedJson).HasConversion(phiConverter);
            e.HasOne(x => x.Document)
             .WithMany(d => d.ExtractedData)
             .HasForeignKey(x => x.DocumentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PatientProfile360 ─────────────────────────────────────────────
        modelBuilder.Entity<PatientProfile360>(e =>
        {
            e.ToTable("patient_profiles_360");
            e.Property(p => p.EncryptedSummaryJson).HasColumnType("text");
            e.Property(p => p.DeduplicationStatus).HasMaxLength(20).IsRequired()
             .HasDefaultValue("Pending");
            e.HasIndex(p => p.PatientId).IsUnique();
            e.HasOne(p => p.Patient)
             .WithOne(u => u.PatientProfile)
             .HasForeignKey<PatientProfile360>(p => p.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MergedClinicalEntry ───────────────────────────────────────────
        modelBuilder.Entity<MergedClinicalEntry>(e =>
        {
            e.ToTable("merged_clinical_entries");
            e.Property(m => m.SectionType).HasMaxLength(50).IsRequired();
            e.Property(m => m.EncryptedLabel).HasColumnType("text");
            e.Property(m => m.EncryptedCanonicalValue).HasColumnType("text");
            e.Property(m => m.SourceDocumentIds).HasColumnType("text").IsRequired();
            if (phiConverter is not null)
            {
                e.Property(m => m.EncryptedLabel).HasConversion(phiConverter);
                e.Property(m => m.EncryptedCanonicalValue).HasConversion(phiConverter);
            }
            // Indexed query on PatientId for P95 ≤ 500 ms target (AC-005, NFR-004)
            e.HasIndex(m => m.PatientId).HasDatabaseName("IX_MergedClinicalEntry_PatientId");
            e.HasOne(m => m.Patient)
             .WithMany()
             .HasForeignKey(m => m.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DataConflict ──────────────────────────────────────────────────
        modelBuilder.Entity<DataConflict>(e =>
        {
            e.ToTable("data_conflicts");
            e.Property(c => c.Status).HasMaxLength(30).IsRequired().HasDefaultValue("Open");
            e.Property(c => c.Severity).HasMaxLength(10).IsRequired().HasDefaultValue("Medium");
            e.Property(c => c.ConflictingValues).HasColumnType("text").IsRequired().HasDefaultValue("[]");
            e.Property(c => c.CanonicalValue).HasColumnType("text");
            // xmin shadow property — PostgreSQL system column; guards against concurrent resolution (AC-002)
            e.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
            // Composite index for efficient status-filtered queries per patient (AC-001, NFR)
            e.HasIndex(c => new { c.PatientId, c.Status })
             .HasDatabaseName("IX_DataConflict_PatientId_Status");
            e.HasOne(c => c.Patient)
             .WithMany()
             .HasForeignKey(c => c.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MedicalCodeSuggestion ─────────────────────────────────────────
        modelBuilder.Entity<MedicalCodeSuggestion>(e =>
        {
            e.ToTable("medical_code_suggestions");
            e.Property(m => m.Rank).HasColumnName("rank").IsRequired();
            e.Property(m => m.Status)
             .HasColumnName("status")
             .HasMaxLength(32)
             .HasDefaultValue("Pending")
             .IsRequired();
            e.Property(m => m.ModelVersion)
             .HasColumnName("model_version")
             .HasMaxLength(128)
             .IsRequired();
            e.Property(m => m.PromptHash)
             .HasColumnName("prompt_hash")
             .HasMaxLength(64)
             .IsRequired();
            e.HasIndex(m => new { m.ClinicalDataId, m.Status })
             .HasDatabaseName("IX_MedicalCodeSuggestion_ClinicalDataId_Status");
            e.HasOne(m => m.ClinicalData)
             .WithMany(x => x.CodeSuggestions)
             .HasForeignKey(m => m.ClinicalDataId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Patient)
             .WithMany()
             .HasForeignKey(m => m.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ──────────────────────────────────────────────────────
        // ActorId intentionally has NO FK — the actor user may be deleted.
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.Property(a => a.ActorEmail).HasMaxLength(256).IsRequired();
            e.Property(a => a.Action).HasMaxLength(256).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(256).IsRequired();
            e.Property(a => a.IpAddress).HasMaxLength(45);
        });

        // ── Notification ──────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasOne(n => n.Recipient)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.RecipientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── InsuranceRecord ───────────────────────────────────────────────
        modelBuilder.Entity<InsuranceRecord>(e =>
        {
            e.ToTable("insurance_records");
            e.Property(r => r.ProviderName).HasMaxLength(256).IsRequired();
            e.Property(r => r.InsuranceIdPattern).HasMaxLength(512).IsRequired();
            e.Property(r => r.PlanName).HasMaxLength(256);
            e.Property(r => r.ContactPhone).HasMaxLength(30);
        });

        // ── CalendarSync ──────────────────────────────────────────────────
        modelBuilder.Entity<CalendarSync>(e =>
        {
            e.ToTable("calendar_syncs");
            e.Property(c => c.EncryptedAccessToken).HasColumnType("text");
            e.Property(c => c.EncryptedRefreshToken).HasColumnType("text");
            e.Property(c => c.SyncStatus).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            e.Property(c => c.CalendarEventId).HasMaxLength(512);
            if (phiConverter is not null)
            {
                e.Property(c => c.EncryptedAccessToken).HasConversion(phiConverter);
                e.Property(c => c.EncryptedRefreshToken).HasConversion(phiConverter);
            }
            e.HasOne(c => c.User)
             .WithMany(u => u.CalendarSyncs)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── VerifiedMedicalCode ───────────────────────────────────────────
        modelBuilder.Entity<VerifiedMedicalCode>(e =>
        {
            e.ToTable("verified_medical_codes");
            e.Property(v => v.Decision)
             .HasColumnName("decision")
             .HasMaxLength(32)
             .HasDefaultValue("")
             .IsRequired();
            e.Property(v => v.OriginalSuggestedCode)
             .HasColumnName("original_suggested_code")
             .HasMaxLength(32);
            e.HasOne(v => v.Suggestion)
             .WithOne(m => m.VerifiedCode)
             .HasForeignKey<VerifiedMedicalCode>(v => v.SuggestionId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.VerifiedBy)
             .WithMany()
             .HasForeignKey(v => v.VerifiedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed reference data
        InsuranceRecordSeeder.Seed(modelBuilder);
    }
}
