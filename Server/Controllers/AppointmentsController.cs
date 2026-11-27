using Microsoft.AspNetCore.Mvc;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<Appointment>>> GetAppointments()
    {
        var appointments = await _service.GetAppointmentAsync();
        return Ok(appointments);
    }

    // POST: api/Appointments
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