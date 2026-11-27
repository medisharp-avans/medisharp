using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class TreatmentCodeConfiguration : IEntityTypeConfiguration<TreatmentCode>
    {
        public void Configure(EntityTypeBuilder<TreatmentCode> builder)
        {
            builder.HasKey(t => t.Code);

            builder.Property(t => t.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(t => t.Specialism)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(t => t.DurationInQuarters)
                .IsRequired();

            builder.Property(t => t.Cost)
                .IsRequired()
                .HasPrecision(10, 2);

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_TreatmentCodes_DurationInQuarters",
                    "[DurationInQuarters] > 0");

                t.HasCheckConstraint(
                    "CK_TreatmentCodes_Cost",
                    "[Cost] >= 0");
            });
        }
    }
}