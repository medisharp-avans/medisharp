using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Filters;
using Server.Services;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>Accepts referral requests while account authorization is being developed in PRI-03.</summary>
    [ApiController]
    [Route("api/referrals")]
    [ServiceFilter(typeof(DevelopmentOnlyFilter))]
    public class ReferralsController(IReferralService service) : ControllerBase
    {
        /// <summary>Creates a referral with status Open in the local development environment.</summary>
        /// <remarks>Account authorization depends on PRI-03. Attachments and appointment planning are not input fields.</remarks>
        [HttpPost]
        [ProducesResponseType<CreatedReferralResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
