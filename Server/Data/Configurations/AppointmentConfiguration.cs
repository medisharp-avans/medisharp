using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.PatientId)
                .IsRequired();

            builder.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(a => a.SpecialistId)
                .IsRequired();

            builder.HasOne(a => a.Specialist)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SpecialistId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(a => a.Referral)
                .WithOne(r => r.Appointment)
                .HasForeignKey<Appointment>(a => a.ReferralId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(a => a.StartTime)
                .IsRequired();

            builder.Property(a => a.EndTime)
                .IsRequired();

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Appointments_EndTime",
                "[EndTime] > [StartTime]"));

            builder.HasIndex(a => new { a.SpecialistId, a.StartTime })
                .IsUnique();

            builder.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>();
        }
    }
}