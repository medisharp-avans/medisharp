using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Services.InterFaces
{
    /// <summary>
    /// Provides business logic operations related to managing referrals.
    /// </summary>
    public interface IReferralService
    {
        /// <summary>
        /// Creates a new referral record asynchronously using the provided creation data transfer object.
        /// </summary>
        /// <param name="dto">The data transfer object containing referral details.</param>
        /// <returns>The newly created <see cref="Referral"/> entity.</returns>
        Task<Referral> CreateAsync(CreateReferralRequestDTO dto);
    }
}