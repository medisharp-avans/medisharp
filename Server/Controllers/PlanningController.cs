using Microsoft.AspNetCore.Mvc;
using Server.Models;
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
            return Ok(await _service.GetPatientsAsync());
        }

        [HttpGet("treatmentcodes")]
        public async Task<ActionResult<List<TreatmentCode>>> GetTreatmentCodesBySpecialist([FromQuery] string specialism)
        {
            return Ok(await _service.GetCodesBySpecialist(specialism));
        }

        [HttpGet("search/specialists")]
        public async Task<ActionResult<List<PlanningResponseDTO>>> GetSpecialistsBySpecialism([FromQuery] string specialism)
        {
            return Ok(await _service.GetSpecialistsBySpecialism(specialism));
        }
    }
}
