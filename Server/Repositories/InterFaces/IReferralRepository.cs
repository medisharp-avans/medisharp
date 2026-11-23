using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>Persists referral snapshots without exposing database operations to the service.</summary>
    public interface IReferralRepository
    {
        /// <summary>Saves a new referral and completes only after the database operation succeeds.</summary>
        Task AddAsync(Referral referral, CancellationToken cancellationToken);
    }
}
