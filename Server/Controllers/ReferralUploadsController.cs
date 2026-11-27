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
