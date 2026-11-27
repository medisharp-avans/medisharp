using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Services.InterFaces;

namespace Server.Controllers;

/// <summary>Prefills the referral form for an authenticated general practitioner.</summary>
[ApiController]
[Route("api/referrals/patient-lookup")]
[Authorize(Roles = nameof(UserRole.Huisarts))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReferralPatientLookupController(IReferralIntakeService referrals) : ControllerBase
{
    /// <summary>Finds one patient by exact BSN.</summary>
    /// <remarks>The BSN is supplied in the POST body. A missing record returns HTTP 204 so the GP can enter a new patient.</remarks>
    [HttpPost]
    [ProducesResponseType<ReferralPatientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReferralPatientResponse>> Find(
        PatientLookupRequest request, CancellationToken cancellationToken)
    {
        var patient = await referrals.FindPatientByBsnAsync(request.Bsn, cancellationToken);
        return patient is null ? NoContent() : Ok(patient);
    }
}
