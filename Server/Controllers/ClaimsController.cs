using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Controllers;
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
        /// Retrieves all claims. Claims with status ToReview come first.
        /// </summary>
        /// <returns>A list of <see cref="ClaimOverviewDTO"/>.</returns>
        /// <response code="200">Returns the claims overview.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClaimOverviewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClaimOverviewDTO>>> GetOverview()
        {
            return Ok(await _service.GetOverviewAsync());
        }
    }
}
