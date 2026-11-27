using Referral = Server.Models.Intake.Referral;
using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>Persists referral snapshots without exposing database operations to the service.</summary>
    public interface IReferralIntakeRepository
    {
        /// <summary>Saves a new referral and completes only after the database operation succeeds.</summary>
        Task AddAsync(Referral referral, CancellationToken cancellationToken);
    }
}
