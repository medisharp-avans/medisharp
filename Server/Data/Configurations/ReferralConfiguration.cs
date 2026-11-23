using Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    /// <summary>Maps referral snapshots, field limits and attachment ownership to the database.</summary>
    public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Referral> builder)
        {
            builder.ToTable("Referrals");
            builder.HasKey(referral => referral.Id);

            // Keep the BSN as text so leading zeroes are preserved.
            builder.Property(referral => referral.Bsn).HasMaxLength(9).IsUnicode(false).IsRequired();
            builder.Property(referral => referral.PatientName).HasMaxLength(200).IsRequired();
            builder.Property(referral => referral.PatientAddress).HasMaxLength(300).IsRequired();
            builder.Property(referral => referral.PatientCity).HasMaxLength(100).IsRequired();
            builder.Property(referral => referral.PatientPhoneNumber).HasMaxLength(32).IsRequired();
            builder.Property(referral => referral.PatientEmailAddress).HasMaxLength(254).IsRequired();

            builder.Property(referral => referral.Specialty).HasMaxLength(100).IsRequired();
            builder.Property(referral => referral.TreatmentCode).HasMaxLength(50).IsRequired();
            builder.Property(referral => referral.ReasonForReferral).IsRequired();
            builder.Property(referral => referral.ClinicalQuestion).IsRequired();
            builder.Property(referral => referral.Medication).IsRequired();
            builder.Property(referral => referral.MedicalInformation).IsRequired();
            builder.Property(referral => referral.Allergies).IsRequired();

            builder.Property(referral => referral.Urgency)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(referral => referral.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(ReferralStatus.Open)
                .IsRequired();

            builder.Property(referral => referral.CreatedAtUtc).IsRequired();

            builder.HasIndex(referral => new { referral.Status, referral.CreatedAtUtc });

            builder.HasMany(referral => referral.Attachments)
                .WithOne(attachment => attachment.Referral)
                .HasForeignKey(attachment => attachment.ReferralId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
