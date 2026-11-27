using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services.InterFaces;
using static Server.Controllers.PlanningController;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _service;

        public PatientsController(IPatientService service)
        {
            _service = service;
        }
        [HttpGet("search/patients")]
        public async Task<ActionResult<List<Patient>>> SearchPatients()
        {
            var patients = await _service.GetPatientsAsync();

            if (!patients.Any()) return NotFound();

            return Ok(patients);
        }
    }
}
