using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class ReferralController : ControllerBase
    {
        private readonly IReferralService _service;

        public ReferralController(IReferralService service)
        {
            _service = service;
        }

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