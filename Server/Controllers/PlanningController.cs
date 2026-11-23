using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService _service;

        public PlanningController(IPlanningService service)
        {
            _service = service;
        }
        [HttpGet("search/patients")]
        public async Task<ActionResult<List<PlanningResponseDTO>>> SearchPatients()
        {
            var patients = await _service.GetPatientsAsync();

            if (!patients.Any()) return NotFound();

            return Ok(patients);
        }
    }
}
