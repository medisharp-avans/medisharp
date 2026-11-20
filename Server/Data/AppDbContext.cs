using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    /// <summary>
    /// EF Core database context for MediSharp. Add the DbSets for the entities here,
    /// then create a migration with: dotnet ef migrations add &lt;Name&gt;
    /// </summary>
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Specialist> Specialists { get; set; }
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<Appointment> Appointments { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Patient (1-op-1)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne(u => u.PatientDetails)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // User - Specialist (1-op-1)
            modelBuilder.Entity<Specialist>()
                .HasOne(s => s.User)
                .WithOne(u => u.SpecialistDetails)
                .HasForeignKey<Specialist>(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // User - GeneralPractitioner (1-op-1)
            modelBuilder.Entity<GeneralPractitioner>()
                .HasOne(g => g.User)
                .WithOne(u => u.GeneralPractitionerDetails)
                .HasForeignKey<GeneralPractitioner>(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // GeneralPractitioner - Patient (1-op-veel)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.GeneralPractitioner)
                .WithMany(g => g.Patients)
                .HasForeignKey(p => p.GeneralPractitionerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Patient - Referral (1-op-veel)
            modelBuilder.Entity<Referral>()
                .HasOne(r => r.Patient)
                .WithMany(p => p.Referrals)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // GeneralPractitioner - Referral (1-op-veel)
            modelBuilder.Entity<Referral>()
                .HasOne(r => r.GeneralPractitioner)
                .WithMany(g => g.Referrals)
                .HasForeignKey(r => r.GeneralPractitionerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Specialist - PreferredSpecialist (1-op-veel)
            // Geen navigatiecollectie nodig in Specialist
            modelBuilder.Entity<Referral>()
                .HasOne(r => r.PreferredSpecialist)
                .WithMany()
                .HasForeignKey(r => r.PreferredSpecialistId)
                .OnDelete(DeleteBehavior.NoAction);

            // Patient - Appointment (1-op-veel)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // Specialist - Appointment (1-op-veel)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Specialist)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SpecialistId)
                .OnDelete(DeleteBehavior.NoAction);

            // Referral - Appointment (1-op-1, optioneel)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Referral)
                .WithOne(r => r.Appointment)
                .HasForeignKey<Appointment>(a => a.ReferralId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        }

    }
}
