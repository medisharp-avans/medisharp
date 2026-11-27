using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Services;
using System.Text.Json;

namespace Server.Controllers;

/// <summary>Accepts a referral and optional attachments from an authenticated general practitioner.</summary>
[ApiController]
[Route("api/referrals/with-attachments")]
[Authorize(Roles = nameof(UserRole.Huisarts))]
public class ReferralUploadsController(ReferralUploadService uploads) : ControllerBase
{
    /// <summary>Creates an open referral and stores the submitted attachments.</summary>
    /// <param name="referral">The JSON-encoded referral request in the multipart form.</param>
    /// <param name="attachments">Up to five PDF, JPEG or PNG files, at most 10 MiB per file and 25 MiB combined.</param>
    /// <param name="cancellationToken">Cancels the operation when the request is aborted.</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(28 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 28 * 1024 * 1024)]
    [ProducesResponseType<CreatedReferralResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreatedReferralResponse>> Create([FromForm] string referral,
        [FromForm] List<IFormFile> attachments, CancellationToken cancellationToken)
    {
        CreateReferralRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<CreateReferralRequest>(referral, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            request = null;
        }

        if (request is null)
        {
            ModelState.AddModelError(nameof(referral), "Ongeldige verwijzingsgegevens.");
            return ValidationProblem(ModelState);
        }

        var result = await uploads.CreateAsync(request, attachments, cancellationToken);
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
