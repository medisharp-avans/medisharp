using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data.Configurations
{
    public class ClaimLineConfiguration : IEntityTypeConfiguration<ClaimLine>
    {
        public void Configure(EntityTypeBuilder<ClaimLine> builder)
        {
            builder.ToTable("claim_lines", table =>
            {
                table.HasCheckConstraint("CK_claim_lines_submitted_amount", "[submitted_amount] >= 0");
                table.HasCheckConstraint("CK_claim_lines_reimbursed_amount", "[reimbursed_amount] IS NULL OR [reimbursed_amount] BETWEEN 0 AND [submitted_amount]");
                table.HasCheckConstraint("CK_claim_lines_submission_count", "[submission_count] >= 0");
                table.HasCheckConstraint("CK_claim_lines_status", DbEnum<ClaimLineStatus>.CheckSql("status"));
                table.HasCheckConstraint("CK_claim_lines_rejection_reason",
                    $"[rejection_reason] IS NULL OR {DbEnum<ClaimRejectionReason>.CheckSql("rejection_reason")}");
            });

            builder.HasKey(cl => cl.Id);
            builder.Property(cl => cl.Id).HasColumnName("id");
            builder.Property(cl => cl.ClaimId).HasColumnName("claim_id");
            builder.Property(cl => cl.AppointmentId).HasColumnName("appointment_id");
            builder.Property(cl => cl.SubmittedAmount).HasColumnName("submitted_amount").HasPrecision(10, 2);
            builder.Property(cl => cl.ReimbursedAmount).HasColumnName("reimbursed_amount").HasPrecision(10, 2);
            builder.Property(cl => cl.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(30).HasConversion<string>();
            builder.Property(cl => cl.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>();
            builder.Property(cl => cl.SubmittedAt).HasColumnName("submitted_at");
            builder.Property(cl => cl.SubmissionCount).HasColumnName("submission_count");

            // An appointment is claimed at most once.
            builder.HasIndex(cl => cl.AppointmentId)
                .IsUnique()
                .HasDatabaseName("UX_claim_lines_appointment_id");

            builder.HasOne(cl => cl.Claim).WithMany(c => c.ClaimLines).HasForeignKey(cl => cl.ClaimId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(cl => cl.Appointment).WithOne().HasForeignKey<ClaimLine>(cl => cl.AppointmentId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
