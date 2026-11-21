using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Data;
using Server.Services.InterFaces;
using Server.Models.DTO.Requests;

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
    public async Task<ActionResult<Appointment>> GetAppointments()
    {
        var appointments = await _service.GetAppointmentAsync();
        return Ok(appointments);
    }
    // POST: api/Appointment
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Appointment>> PostAppointment(AppointmentManualRequestDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var createdAppointment = await _service.CreateAppointment(dto);
            return Ok(createdAppointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)      // eigen exception: bezet of buiten beschikbaarheid
        {
            throw;
        }
    }
}
