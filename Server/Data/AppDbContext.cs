
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
        public DbSet<ExternalCalendarEvent> ExternalCalendarEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
