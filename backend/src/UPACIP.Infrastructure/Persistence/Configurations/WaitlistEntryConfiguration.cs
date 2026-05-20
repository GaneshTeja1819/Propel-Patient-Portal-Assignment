using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence.Configurations;

internal sealed class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable("waitlist_entries");

        builder.HasOne(w => w.Patient)
            .WithMany()
            .HasForeignKey(w => w.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Provider)
            .WithMany()
            .HasForeignKey(w => w.ProviderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Appointment)
            .WithMany()
            .HasForeignKey(w => w.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.PreferredSlot)
            .WithMany()
            .HasForeignKey(w => w.PreferredSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.PatientId, w.PreferredSlotId }).IsUnique();
    }
}
