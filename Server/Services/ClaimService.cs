using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class ClaimService(IClaimRepository _repository) : IClaimService
    {
        /// <inheritdoc />
        public async Task<PagedResult<ClaimOverviewDTO>> GetOverviewPageAsync(PagedQuery query, ClaimStatus? status = null)
        {
            var (items, totalCount) = await _repository.GetOverviewPageAsync(query, status);

            return new PagedResult<ClaimOverviewDTO>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        /// <inheritdoc />
        public Task<int> CountToReviewAsync() => _repository.CountToReviewAsync();
    }
}
