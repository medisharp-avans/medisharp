using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models.Intake;

namespace Server.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.Property(p => p.Bsn).IsUnicode(false).HasMaxLength(9);
        builder.HasIndex(p => p.Bsn).IsUnique();
        builder.Property(p => p.FullName).HasMaxLength(200);
        builder.Property(p => p.PostalCode).HasMaxLength(12);
        builder.Property(p => p.HouseNumber).HasMaxLength(20);
        builder.Property(p => p.PhoneNumber).HasMaxLength(32);
        builder.Property(p => p.Email).HasMaxLength(254);
    }
}
