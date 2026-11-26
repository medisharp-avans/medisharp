using Server.Models;
using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;

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
        /// <returns>The rows of the page and the number of rows matching the search.</returns>
        Task<(List<ClaimOverviewDTO> Items, int TotalCount)> GetOverviewPageAsync(PagedQuery query);

        /// <summary>Counts all claims with status ToReview.</summary>
        Task<int> CountToReviewAsync();
    }
}
