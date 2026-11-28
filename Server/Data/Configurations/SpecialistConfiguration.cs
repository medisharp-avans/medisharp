
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class SpecialistConfiguration : IEntityTypeConfiguration<Specialist>
    {
        public void Configure(EntityTypeBuilder<Specialist> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.UserId)
                .IsRequired();

            builder.HasOne(s => s.User)
                .WithOne(u => u.SpecialistDetails)
                .HasForeignKey<Specialist>(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(s => s.Specialism)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(s => s.EncryptedIcalUrl)
                .IsRequired(false);

            builder.Property(s => s.LastSyncedUtc)
                .IsRequired(false);

            builder.HasIndex(s => s.UserId)
                .IsUnique();
        }
    }
}
