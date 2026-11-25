using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data.Configurations
{
    public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
    {
        public void Configure(EntityTypeBuilder<Claim> builder)
        {
            builder.ToTable("claims", table =>
            {
                table.HasCheckConstraint("CK_claims_status", DbEnum<ClaimStatus>.CheckSql("status"));
                table.HasCheckConstraint("CK_claims_submitted_at", "[submitted_at] IS NULL OR [submitted_at] >= [created_at]");
            });

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.PatientId).HasColumnName("patient_id");
            builder.Property(c => c.BatchId).HasColumnName("batch_id");
            builder.Property(c => c.ReviewedById).HasColumnName("reviewed_by_id");
            builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(30).HasConversion<string>();
            builder.Property(c => c.PolicyNumber).HasColumnName("policy_number").HasMaxLength(30);
            builder.Property(c => c.CreatedAt).HasColumnName("created_at");
            builder.Property(c => c.SubmittedAt).HasColumnName("submitted_at");
            builder.Property(c => c.LastModifiedAt).HasColumnName("last_modified_at");

            // Restrict-style deletes everywhere, like the rest of the model: SQL Server refuses multiple cascade paths.
            builder.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(c => c.Batch).WithMany(b => b.Claims).HasForeignKey(c => c.BatchId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(c => c.ReviewedBy).WithMany(h => h.ReviewedClaims).HasForeignKey(c => c.ReviewedById).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
