
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.UserId)
                .IsRequired();

            builder.Property(p => p.Bsn)
                .IsRequired()
                .HasMaxLength(9);

            builder.HasIndex(p => p.Bsn)
                .IsUnique();

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Patients_Bsn",
                "LEN([Bsn]) = 9 AND [Bsn] NOT LIKE '%[^0-9]%'"));

            builder.Property(p => p.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(p => p.Address)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.City)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.HasInsurance)
                .HasDefaultValue(true);

            builder.HasOne(p => p.User)
                .WithOne(u => u.PatientDetails)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(p => p.GeneralPractitioner)
                .WithMany(g => g.Patients)
                .HasForeignKey(p => p.GeneralPractitionerId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
