using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Data.SeedData;
using Server.Models;

namespace Server.Data.Configurations
{
    /// <summary>Maps the treatment catalogue and its versioned reference data.</summary>
    public class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Treatment> builder)
        {
            builder.ToTable("Treatments");
            builder.HasKey(treatment => treatment.Code);
            builder.Property(treatment => treatment.Code).HasMaxLength(50).IsRequired();
            builder.Property(treatment => treatment.Description).HasMaxLength(200).IsRequired();
            builder.Property(treatment => treatment.Specialty).HasMaxLength(100).IsRequired();
            builder.Property(treatment => treatment.DurationInQuarters).IsRequired();
            builder.Property(treatment => treatment.CostEuro).HasPrecision(18, 2).IsRequired();
            builder.HasIndex(treatment => treatment.Specialty);
            builder.HasData(new TreatmentSeedData().Read());
        }
    }
}
