using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class ClaimBatchConfiguration : IEntityTypeConfiguration<ClaimBatch>
    {
        public void Configure(EntityTypeBuilder<ClaimBatch> builder)
        {
            builder.ToTable("claim_batches");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasColumnName("id");
            builder.Property(b => b.SentById).HasColumnName("sent_by_id");
            builder.Property(b => b.SentAt).HasColumnName("sent_at");

            builder.HasOne(b => b.SentBy).WithMany(h => h.SentClaimBatches).HasForeignKey(b => b.SentById).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
