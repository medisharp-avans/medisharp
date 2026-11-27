using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories;

namespace Server.Controllers;

[ApiController]
[Route("api/referrals/open")]
[Authorize(Roles = nameof(UserRole.Ziekenhuismedewerker))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OpenReferralsController(ReferralIntakeRepository repository) : ControllerBase
{
    /// <summary>Lists open referrals for hospital staff authorized to schedule appointments.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OpenReferralResponse>>(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > int.MaxValue / ReferralIntakeRepository.OpenPageSize) return BadRequest();
        return Ok(await repository.GetOpenPageAsync(page, cancellationToken));
    }
}
