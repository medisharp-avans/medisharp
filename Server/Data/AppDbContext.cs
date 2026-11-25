
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    /// <summary>
    /// EF Core database context for MediSharp.
    /// </summary>
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Specialist> Specialists { get; set; }
        public DbSet<GeneralPractitioner> GeneralPractitioners { get; set; }
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<TreatmentCode> TreatmentCodes { get; set; }
        public DbSet<Availability> Availabilities { get; set; }
        public DbSet<HospitalEmployee> HospitalEmployees { get; set; }

        // Claims and invoices (DEC)
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimBatch> ClaimBatches { get; set; }
        public DbSet<ClaimLine> ClaimLines { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        public DbSet<PaymentPlan> PaymentPlans { get; set; }
        public DbSet<PaymentReminder> PaymentReminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Picks up every IEntityTypeConfiguration in Data/Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.Property(u => u.Role)
                    .HasConversion<string>();
            });

            // TreatmentCode configuratie
            modelBuilder.Entity<TreatmentCode>(e =>
            {
                e.Property(t => t.Cost)
                    .HasPrecision(10, 2);
                e.Property(tc => tc.Specialism)
                    .HasConversion<string>();
            });

            // Patient relaties
            modelBuilder.Entity<Patient>(e =>
            {
                // User - Patient (1-op-1)
                e.HasOne(p => p.User)
                    .WithOne(u => u.PatientDetails)
                    .HasForeignKey<Patient>(p => p.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // GeneralPractitioner - Patient (1-op-veel)
                e.HasOne(p => p.GeneralPractitioner)
                    .WithMany(g => g.Patients)
                    .HasForeignKey(p => p.GeneralPractitionerId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Specialist relaties
            modelBuilder.Entity<Specialist>(e =>
            {
                // User - Specialist (1-op-1)
                e.HasOne(s => s.User)
                    .WithOne(u => u.SpecialistDetails)
                    .HasForeignKey<Specialist>(s => s.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasMany(s => s.Availabilities)
                    .WithOne(a => a.Specialist)
                    .HasForeignKey(a => a.SpecialistId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.Property(s => s.Specialism)
                    .HasConversion<string>();
            });

            // GeneralPractitioner relaties
            modelBuilder.Entity<GeneralPractitioner>(e =>
            {
                // User - GeneralPractitioner (1-op-1)
                e.HasOne(g => g.User)
                    .WithOne(u => u.GeneralPractitionerDetails)
                    .HasForeignKey<GeneralPractitioner>(g => g.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Referral relaties
            modelBuilder.Entity<Referral>(e =>
            {
                // Patient - Referral (1-op-veel)
                e.HasOne(r => r.Patient)
                    .WithMany(p => p.Referrals)
                    .HasForeignKey(r => r.PatientId)
                    .OnDelete(DeleteBehavior.NoAction);

                // GeneralPractitioner - Referral (1-op-veel)
                e.HasOne(r => r.GeneralPractitioner)
                    .WithMany(g => g.Referrals)
                    .HasForeignKey(r => r.GeneralPractitionerId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Specialist - PreferredSpecialist (1-op-veel)
                e.HasOne(r => r.PreferredSpecialist)
                    .WithMany()
                    .HasForeignKey(r => r.PreferredSpecialistId)
                    .OnDelete(DeleteBehavior.NoAction);

                // TreatmentCode - Referral (1-op-veel)
                e.HasOne(r => r.TreatmentCodeDetails)
                    .WithMany()
                    .HasForeignKey(r => r.TreatmentCode)
                    .OnDelete(DeleteBehavior.NoAction);

                e.Property(r => r.Specialism)
                    .HasConversion<string>();
            });

            // Appointment relaties
            modelBuilder.Entity<Appointment>(e =>
            {
                // Patient - Appointment (1-op-veel)
                e.HasOne(a => a.Patient)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Specialist - Appointment (1-op-veel)
                e.HasOne(a => a.Specialist)
                    .WithMany(s => s.Appointments)
                    .HasForeignKey(a => a.SpecialistId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Referral - Appointment (1-op-1, optioneel)
                e.HasOne(a => a.Referral)
                    .WithOne(r => r.Appointment)
                    .HasForeignKey<Appointment>(a => a.ReferralId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(a => new { a.SpecialistId, a.StartTime })
                    .IsUnique();

                e.Property(a => a.Status)
                    .HasConversion<string>();
            });


        }
    }
}
