using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>
    /// Manages planning, schedules, and search endpoints for patients, specialists, and treatment codes.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanningController"/> class.
        /// </summary>
        /// <param name="service">The planning service used for business logic operations.</param>
        public PlanningController(IPlanningService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a list of all patients formatted for planning purposes.
        /// </summary>
        /// <returns>A list of <see cref="PlanningResponseDTO"/> representing the patients.</returns>
        /// <response code="200">Returns the list of patients.</response>
        [HttpGet("search/patients")]
        public async Task<ActionResult<List<PlanningResponseDTO>>> SearchPatients()
        {
            return Ok(await _service.GetPatientsAsync());
        }

        /// <summary>
        /// Retrieves treatment codes associated with a specific specialist or specialism.
        /// </summary>
        /// <param name="specialism">The name or code of the specialism.</param>
        /// <returns>A list of matching <see cref="TreatmentCode"/> entities.</returns>
        /// <response code="200">Returns the list of treatment codes.</response>
        [HttpGet("treatmentcodes")]
        public async Task<ActionResult<List<TreatmentCode>>> GetTreatmentCodesBySpecialist([FromQuery] string specialism)
        {
            return Ok(await _service.GetCodesBySpecialist(specialism));
        }

        /// <summary>
        /// Retrieves specialists associated with a specific medical specialism.
        /// </summary>
        /// <param name="specialism">The name or code of the specialism.</param>
        /// <returns>A list of <see cref="PlanningResponseDTO"/> representing the specialists.</returns>
        /// <response code="200">Returns the list of specialists.</response>
        [HttpGet("search/specialists")]
        public async Task<ActionResult<List<PlanningResponseDTO>>> GetSpecialistsBySpecialism([FromQuery] string specialism)
        {
            return Ok(await _service.GetSpecialistsBySpecialism(specialism));
        }
    }
}