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
            e.HasOne(a => a.Patient)
             .WithMany(u => u.PatientAppointments)
             .HasForeignKey(a => a.PatientId)
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

        // ── WaitlistEntry ─────────────────────────────────────────────────
        modelBuilder.Entity<WaitlistEntry>(e =>
        {
            e.ToTable("waitlist_entries");
            e.HasOne(w => w.Patient)
             .WithMany()
             .HasForeignKey(w => w.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Provider)
             .WithMany()
             .HasForeignKey(w => w.ProviderId)
             .IsRequired(false)
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
            if (phiConverter is not null)
                e.Property(d => d.StoragePath).HasConversion(phiConverter);
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
            e.HasIndex(p => p.PatientId).IsUnique();
            e.HasOne(p => p.Patient)
             .WithOne(u => u.PatientProfile)
             .HasForeignKey<PatientProfile360>(p => p.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DataConflict ──────────────────────────────────────────────────
        modelBuilder.Entity<DataConflict>(e =>
        {
            e.ToTable("data_conflicts");
            e.HasOne(c => c.Patient)
             .WithMany()
             .HasForeignKey(c => c.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MedicalCodeSuggestion ─────────────────────────────────────────
        modelBuilder.Entity<MedicalCodeSuggestion>(e =>
        {
            e.ToTable("medical_code_suggestions");
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
            e.Property(c => c.EncryptedRefreshToken).HasColumnType("text");            if (phiConverter is not null)
            {
                e.Property(c => c.EncryptedAccessToken).HasConversion(phiConverter);
                e.Property(c => c.EncryptedRefreshToken).HasConversion(phiConverter);
            }            e.HasOne(c => c.User)
             .WithMany(u => u.CalendarSyncs)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── VerifiedMedicalCode ───────────────────────────────────────────
        modelBuilder.Entity<VerifiedMedicalCode>(e =>
        {
            e.ToTable("verified_medical_codes");
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
