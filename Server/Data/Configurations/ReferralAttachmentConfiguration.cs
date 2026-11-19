using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class ReferralAttachmentConfiguration : IEntityTypeConfiguration<ReferralAttachment>
    {
        public void Configure(EntityTypeBuilder<ReferralAttachment> builder)
        {
            builder.ToTable("ReferralAttachments");
            builder.HasKey(attachment => attachment.Id);

            builder.Property(attachment => attachment.FileName).HasMaxLength(255).IsRequired();
            builder.Property(attachment => attachment.ContentType).HasMaxLength(127).IsRequired();
            builder.Property(attachment => attachment.StorageKey).HasMaxLength(500).IsRequired();
            builder.Property(attachment => attachment.SizeInBytes).IsRequired();
        }
    }
}
