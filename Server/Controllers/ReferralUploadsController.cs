using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Filters;
using Server.Services;
using Server.Services.InterFaces;
using System.Text.Json;

namespace Server.Controllers;

/// <summary>Creates a referral and optional attachments as a single development-only operation.</summary>
[ApiController]
[Route("api/referrals/with-attachments")]
[ServiceFilter(typeof(DevelopmentOnlyFilter))]
public class ReferralUploadsController(ReferralUploadService uploads) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(28 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 28 * 1024 * 1024)]
    [ProducesResponseType<CreatedReferralResponse>(201)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<IActionResult> Create([FromForm] string referral,
        [FromForm] List<IFormFile> attachments, CancellationToken cancellationToken)
    {
        CreateReferralRequest? request;
        try { request = JsonSerializer.Deserialize<CreateReferralRequest>(referral, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return BadRequest(new ValidationProblemDetails()); }
        if (request is null) return BadRequest(new ValidationProblemDetails());
        var result = await uploads.CreateAsync(request, attachments, cancellationToken);
        return result.Errors.Count > 0 ? BadRequest(new ValidationProblemDetails(result.Errors))
            : StatusCode(201, result.Referral);
    }
}
