using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>
    /// Manages external iCal/ICS calendar integration endpoints for specialists.
    /// </summary>
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly IAgendaService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarController"/> class.
        /// </summary>
        /// <param name="service">The agenda service responsible for calendar synchronization and connection management.</param>
        public CalendarController(IAgendaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Connects an external iCal/ICS calendar feed to a specialist and performs an initial synchronization.
        /// </summary>
        /// <param name="request">The request payload containing the specialist ID and the iCal/ICS URL.</param>
        /// <returns>A confirmation message when the calendar has been connected and synchronized.</returns>
        /// <response code="200">Returns a confirmation message indicating the calendar is connected and synced.</response>
        /// <response code="400">If the request model is invalid or the iCal/ICS URL is unreachable or invalid.</response>
        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectIcalRequestDTO request)
        {
            if (!ModelState.IsValid) return BadRequest();

            try
            {
                await _service.SaveAndSyncUrlAsync(request.SpecialistId, request.IcalUrl);
                return Ok(new { message = "Agenda gekoppeld en gesynchroniseerd." });
            }
            catch (ArgumentException)
            {
                return BadRequest(new { message = "De iCal/ICS-link kon niet worden gevonden." });
            }
        }

        /// <summary>
        /// Retrieves the external calendar connection status for a specific specialist.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>An object indicating whether an external calendar is currently connected.</returns>
        /// <response code="200">Returns the connection status (<c>connected: true/false</c>).</response>
        [HttpGet("status/{specialistId:int}")]
        public async Task<IActionResult> Status(int specialistId)
        {
            return Ok(new { connected = await _service.IsConnectedAsync(specialistId) });
        }

        /// <summary>
        /// Disconnects a specialist's external calendar and removes all associated synced events.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>No content upon successful disconnection.</returns>
        /// <response code="204">The calendar connection and synced events have been removed.</response>
        [HttpDelete("disconnect/{specialistId:int}")]
        public async Task<IActionResult> Disconnect(int specialistId)
        {
            await _service.DisconnectAsync(specialistId);
            return NoContent();
        }
    }
}
