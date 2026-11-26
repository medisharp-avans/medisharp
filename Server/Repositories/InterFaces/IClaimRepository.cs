using Server.Models;
using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;
using Server.Models.Enums;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Provides data access operations specific to <see cref="Claim"/> entities.
    /// </summary>
    public interface IClaimRepository : IRepository<Claim>
    {
        /// <summary>
        /// Retrieves one page of the claims overview, after the search and the sort of <paramref name="query"/>.
        /// SortBy accepts Id, Patient, PolicyNumber, Status, LineCount, TotalAmount, ReviewedBy, CreatedAt and LastModifiedAt.
        /// </summary>
        /// <param name="query">Search, sort and paging options.</param>
        /// <param name="status">Only claims with this status; all claims when null.</param>
        /// <returns>The rows of the page and the number of rows matching the search and status.</returns>
        Task<(List<ClaimOverviewDTO> Items, int TotalCount)> GetOverviewPageAsync(PagedQuery query, ClaimStatus? status = null);

        /// <summary>Counts all claims with status ToReview.</summary>
        Task<int> CountToReviewAsync();
    }
}
