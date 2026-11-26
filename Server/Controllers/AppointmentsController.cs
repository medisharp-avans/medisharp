using Microsoft.AspNetCore.Mvc;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>
    /// Manages appointment-related HTTP endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppointmentsController"/> class.
        /// </summary>
        /// <param name="service">The appointment service used for business logic processing.</param>
        public AppointmentsController(IAppointmentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new manual appointment.
        /// </summary>
        /// <param name="dto">The data transfer object containing the manual appointment details.</param>
        /// <returns>The created appointment data transfer object.</returns>
        /// <response code="200">Returns the newly created appointment.</response>
        /// <response code="400">If the model state is invalid.</response>
        /// <response code="404">If the associated patient is not found.</response>
        /// <response code="409">If the requested time slot is unavailable.</response>
        [HttpPost]
        public async Task<ActionResult<AppointmentResponseDTO>> PostAppointment(AppointmentManualRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var a = await _service.CreateAppointment(dto);
                return Ok(new AppointmentResponseDTO(
                    a.Id, a.PatientId, a.SpecialistId, a.ReferralId, a.StartTime, a.EndTime, a.Status));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (SlotUnavailableException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}