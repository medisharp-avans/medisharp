using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Models;
using Server.Services;
using Server.Services.InterFaces;
using System.Security.Claims;

namespace Server.Controllers 
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly IAgendaService _service;

        public CalendarController(IAgendaService service)
        {
            _service = service;
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectIcalRequest request)
        {
            if (!ModelState.IsValid) return BadRequest();

            try
            {
                await _service.SaveAndSyncUrlAsync(request.SpecialistId, request.IcalUrl);
                return Ok(new { message = "Agenda gekoppeld en gesynchroniseerd." });

            } catch (ArgumentException)
            {
                return BadRequest(new { message = "De iCal/ICS-link kon niet worden gevonden." });
            }    
        }

        [HttpGet("status/{specialistId:int}")]
        public async Task<IActionResult> Status(int specialistId)
        {
            return Ok(new { connected = await _service.IsConnectedAsync(specialistId) });
        }

        [HttpDelete("disconnect/{specialistId:int}")]
        public async Task<IActionResult> Disconnect(int specialistId)
        {
            await _service.DisconnectAsync(specialistId);
            return NoContent();
        }
    }

    public record ConnectIcalRequest(int SpecialistId, string IcalUrl);
}
