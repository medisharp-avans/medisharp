using Server.Models;

namespace Server.Data.Repositories
{
    public interface IReferralRepository
    {
        Task AddAsync(Referral referral, CancellationToken cancellationToken);
    }
}
