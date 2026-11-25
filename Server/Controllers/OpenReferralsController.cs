using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Responses;
using Server.Repositories;

namespace Server.Controllers;

[ApiController]
[Route("api/referrals/open")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OpenReferralsController(ReferralRepository repository) : ControllerBase
{
    /// <summary>Lists open referrals for hospital staff authorized to schedule appointments.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OpenReferralResponse>>(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        // PRI-03 must supply the authenticated principal and this permission claim.
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        if (!User.HasClaim("permission", "appointments:schedule")) return StatusCode(403);
        if (page is < 1 or > int.MaxValue / ReferralRepository.OpenPageSize) return BadRequest();
        return Ok(await repository.GetOpenPageAsync(page, cancellationToken));
    }
}
