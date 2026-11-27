using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories;

namespace Server.Controllers;

[ApiController]
[Route("api/referrals/patient-lookup")]
[Authorize(Roles = nameof(UserRole.Huisarts))]
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
        var patient = await patients.FindByBsnAsync(request.Bsn, cancellationToken);
        return patient is null ? NoContent() : Ok(patient);
    }
}
