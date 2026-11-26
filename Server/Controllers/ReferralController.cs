using Microsoft.AspNetCore.Mvc;
using Server.Controllers;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>
    /// Manages referral-related HTTP endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReferralController(IReferralService _service) : BaseApiController
    {
        /// <summary>
        /// Creates a new referral record.
        /// </summary>
        /// <param name="dto">The data transfer object containing referral creation details.</param>
        /// <returns>The newly created <see cref="Referral"/> entity.</returns>
        /// <response code="200">Returns the successfully created referral.</response>
        /// <response code="400">If the model state is invalid.</response>
        [HttpPost]
        public async Task<IActionResult> Create(CreateReferralRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(dto);

            try
            {
                var referral = await _service.CreateAsync(dto);
                return Ok(referral);
                //return CreatedAtAction(nameof(Create), new { id = referral.Id }, referral);
            }
            catch
            {
                throw;
            }
        }
    }
}