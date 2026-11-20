using Server.Models;

namespace Server.Data.Repositories
{
    public class ReferralRepository(AppDbContext context) : IReferralRepository
    {
        public async Task AddAsync(Referral referral, CancellationToken cancellationToken)
        {
            context.Referrals.Add(referral);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
