using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>
    /// Claims (declaraties) for the hospital administration (user story DEC-01).
    /// </summary>
    [Route("api/[controller]")]
    // TODO: login does not work yet. Replace [AllowAnonymous] with
    // [Authorize(Roles = nameof(UserRole.Administratiemedewerker))] once it does (AC-DEC-01_3).
    [AllowAnonymous]
    public class ClaimsController(IClaimService _service) : BaseApiController
    {
        /// <summary>
        /// Retrieves one page of the claims overview, with optional search and sort.
        /// SortBy: Id, Patient, PolicyNumber, Status, LineCount, TotalAmount, ReviewedBy, CreatedAt or LastModifiedAt.
        /// Without a sort column, claims with status ToReview come first.
        /// </summary>
        /// <param name="query">Search term, sort column and direction, page and page size.</param>
        /// <response code="200">Returns one page of the claims overview.</response>
        /// <response code="400">Invalid search or paging values.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ClaimOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<ClaimOverviewDTO>>> GetOverview([FromQuery] PagedQuery query)
        {
            return Ok(await _service.GetOverviewPageAsync(query));
        }

        /// <summary>Counts all claims that still need to be reviewed, for the notice above the overview.</summary>
        /// <response code="200">Returns the number of claims with status ToReview.</response>
        [HttpGet("to-review-count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> GetToReviewCount()
        {
            return Ok(await _service.CountToReviewAsync());
        }
    }
}
