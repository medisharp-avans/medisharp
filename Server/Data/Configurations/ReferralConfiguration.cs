using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data.Configurations
{
    public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
    {
        public void Configure(EntityTypeBuilder<Referral> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.PatientId)
                .IsRequired();

            builder.HasOne(r => r.Patient)
                .WithMany(p => p.Referrals)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(r => r.GeneralPractitionerId)
                .IsRequired();

            builder.HasOne(r => r.GeneralPractitioner)
                .WithMany(g => g.Referrals)
                .HasForeignKey(r => r.GeneralPractitionerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(r => r.Specialism)
                .IsRequired()
                .HasConversion<string>();

            builder.HasOne(r => r.PreferredSpecialist)
                .WithMany()
                .HasForeignKey(r => r.PreferredSpecialistId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(r => r.Reason)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(r => r.Questionnaire)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(r => r.Urgency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ReferralUrgency.Regulier);

            builder.Property(r => r.TreatmentCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasOne(r => r.TreatmentCodeDetails)
                .WithMany()
                .HasForeignKey(r => r.TreatmentCode)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(r => r.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Open");

            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
