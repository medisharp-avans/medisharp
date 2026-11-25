using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class HospitalEmployeeConfiguration : IEntityTypeConfiguration<HospitalEmployee>
    {
        public void Configure(EntityTypeBuilder<HospitalEmployee> builder)
        {
            builder.ToTable("hospital_employees");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Id).HasColumnName("id");
            builder.Property(h => h.UserId).HasColumnName("user_id");
            builder.Property(h => h.Department).HasColumnName("department").HasMaxLength(100);

            builder.HasIndex(h => h.UserId)
                .IsUnique()
                .HasDatabaseName("UX_hospital_employees_user_id");

            builder.HasOne(h => h.User).WithOne(u => u.HospitalEmployeeDetails).HasForeignKey<HospitalEmployee>(h => h.UserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
