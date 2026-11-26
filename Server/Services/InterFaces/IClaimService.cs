using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    /// <summary>
    /// Business logic for claims (declaraties) that are sent to the insurer.
    /// </summary>
    public interface IClaimService
    {
        /// <summary>
        /// Retrieves the claims overview. Claims that still need to be reviewed come first,
        /// then the rest; within each group the most recently changed claim comes first.
        /// </summary>
        /// <returns>The sorted overview rows.</returns>
        Task<List<ClaimOverviewDTO>> GetOverviewAsync();
    }
}
