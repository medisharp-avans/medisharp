
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class GeneralPractitionerConfiguration : IEntityTypeConfiguration<GeneralPractitioner>
    {
        public void Configure(EntityTypeBuilder<GeneralPractitioner> builder)
        {
            builder.HasKey(g => g.Id);

            builder.Property(g => g.UserId)
                .IsRequired();

            builder.HasIndex(g => g.UserId)
                .IsUnique();

            builder.HasOne(g => g.User)
                .WithOne(u => u.GeneralPractitionerDetails)
                .HasForeignKey<GeneralPractitioner>(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
