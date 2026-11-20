using Server.Models;

namespace Server.Data.Repositories
{
    /// <summary>Persists referrals through the request's scoped EF Core context.</summary>
    public class ReferralRepository(AppDbContext context) : IReferralRepository
    {
        /// <inheritdoc />
        public async Task AddAsync(Referral referral, CancellationToken cancellationToken)
        {
            context.Referrals.Add(referral);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
