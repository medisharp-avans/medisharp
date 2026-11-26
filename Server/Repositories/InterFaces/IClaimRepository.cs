using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Provides data access operations specific to <see cref="Claim"/> entities.
    /// </summary>
    public interface IClaimRepository : IRepository<Claim>
    {
        /// <summary>
        /// Retrieves all claims, including the patient (with user) and the claim lines.
        /// </summary>
        /// <returns>All claims, in no particular order.</returns>
        Task<List<Claim>> GetAllWithDetailsAsync();
    }
}
