using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    /// <summary>
    /// EF Core database context for MediSharp.
    /// </summary>
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Referral> Referrals => Set<Referral>();

        public DbSet<ReferralAttachment> ReferralAttachments => Set<ReferralAttachment>();

        public DbSet<Treatment> Treatments => Set<Treatment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
