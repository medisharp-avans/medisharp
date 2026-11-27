using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>Accepts referral requests from authenticated general practitioners.</summary>
    [ApiController]
    [Route("api/referrals")]
    [Authorize(Roles = nameof(UserRole.Huisarts))]
    public class ReferralsController(IReferralIntakeService service) : ControllerBase
    {
        /// <summary>Creates a referral with status Open.</summary>
        [HttpPost]
        [ProducesResponseType<CreatedReferralResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreatedReferralResponse>> Create(
            CreateReferralRequest request, CancellationToken cancellationToken)
        {
            var result = await service.CreateAsync(request, cancellationToken);
            if (result.Errors.Count > 0)
            {
                return BadRequest(new ValidationProblemDetails(result.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Controleer de ingevulde gegevens"
                });
            }

            return StatusCode(StatusCodes.Status201Created, result.Referral);
        }
    }
}
