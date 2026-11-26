using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;
using Server.Models.Enums;

namespace Server.Services.InterFaces
{
    /// <summary>
    /// Business logic for claims (declaraties) that are sent to the insurer.
    /// </summary>
    public interface IClaimService
    {
        /// <summary>
        /// Retrieves one page of the claims overview. Without a sort column, claims that still need
        /// to be reviewed come first, then the most recently changed claim.
        /// </summary>
        /// <param name="query">Search, sort and paging options.</param>
        /// <param name="status">Only claims with this status; all claims when null.</param>
        /// <returns>The page, with the paging numbers.</returns>
        Task<PagedResult<ClaimOverviewDTO>> GetOverviewPageAsync(PagedQuery query, ClaimStatus? status = null);

        /// <summary>Counts all claims that still need to be reviewed.</summary>
        Task<int> CountToReviewAsync();
    }
}
