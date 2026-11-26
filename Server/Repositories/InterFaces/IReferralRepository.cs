using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Provides data access operations specific to <see cref="Referral"/> entities.
    /// </summary>
    public interface IReferralRepository : IRepository<Referral>
    {
        /// <summary>
        /// Creates a new referral record using the provided creation data transfer object.
        /// </summary>
        /// <param name="dto">The data transfer object containing referral details.</param>
        /// <returns>The newly created <see cref="Referral"/> entity.</returns>
        Task<Referral> CreateAsync(CreateReferralRequestDTO dto);
    }
}