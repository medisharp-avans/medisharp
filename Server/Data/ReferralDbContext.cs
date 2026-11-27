using Patient = Server.Models.Intake.Patient;
using Referral = Server.Models.Intake.Referral;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    /// <summary>
    /// EF Core database context for MediSharp.
    /// </summary>
    public class ReferralDbContext(DbContextOptions<ReferralDbContext> options) : DbContext(options)
    {
        public DbSet<Referral> Referrals => Set<Referral>();

        public DbSet<Patient> Patients => Set<Patient>();

        public DbSet<ReferralAttachment> ReferralAttachments => Set<ReferralAttachment>();

        public DbSet<Treatment> Treatments => Set<Treatment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReferralDbContext).Assembly);
        }
    }
}
