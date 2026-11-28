using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Services.InterFaces;

namespace Server.Controllers;

/// <summary>Exposes the queue of open referrals to hospital staff.</summary>
[ApiController]
[Route("api/referrals/open")]
[Authorize(Roles = nameof(UserRole.Ziekenhuismedewerker))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OpenReferralsController(IReferralIntakeService referrals) : ControllerBase
{
    /// <summary>Returns one page of open referrals, oldest first.</summary>
    /// <param name="request">Page number starting at 1, with up to 25 referrals per page.</param>
    /// <param name="cancellationToken">Cancels the query when the request is aborted.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OpenReferralResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<OpenReferralResponse>>> Get(
        [FromQuery] OpenReferralsRequest request, CancellationToken cancellationToken) =>
        Ok(await referrals.GetOpenReferralsAsync(request.Page, cancellationToken));
}
