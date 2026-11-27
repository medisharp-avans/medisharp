using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Repositories;

namespace Server.Controllers;

[ApiController]
[Route("api/referrals/patient-lookup")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReferralPatientLookupController(PatientLookupRepository patients) : ControllerBase
{
    /// <summary>Finds one patient by exact BSN. The POST body keeps the BSN out of the URL.</summary>
    [HttpPost]
    [ProducesResponseType<ReferralPatientResponse>(200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Find(PatientLookupRequest request, CancellationToken cancellationToken)
    {
        // The shared login middleware supplies this principal after PRI-03/04 integration.
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        if (!User.IsInRole("Huisarts")) return StatusCode(403);
        var patient = await patients.FindByBsnAsync(request.Bsn, cancellationToken);
        return patient is null ? NoContent() : Ok(patient);
    }
}
