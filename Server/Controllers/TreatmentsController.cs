using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;
using System.ComponentModel.DataAnnotations;

namespace Server.Controllers
{
    /// <summary>Exposes read-only reference data for selecting a referral's specialty and treatment.</summary>
    [ApiController]
    [Route("api/treatments")]
    public class TreatmentsController(ITreatmentCatalogService service) : ControllerBase
    {
        /// <summary>Lists the specialties in the treatment catalogue.</summary>
        /// <remarks>This reference data contains no patient records.</remarks>
        [HttpGet("specialties")]
        [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<string>>> GetSpecialties(CancellationToken cancellationToken) =>
            Ok(await service.GetSpecialtiesAsync(cancellationToken));

        /// <summary>Lists treatments for the selected specialty, ordered by description and code.</summary>
        /// <remarks>Use a specialty returned by /api/treatments/specialties. An unknown specialty returns an empty list.</remarks>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<TreatmentResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<TreatmentResponse>>> GetTreatments(
            [FromQuery, Required(ErrorMessage = "Kies een specialisme"),
             StringLength(100, ErrorMessage = "Het specialisme mag maximaal 100 tekens bevatten")] string specialty,
            CancellationToken cancellationToken) =>
            Ok(await service.GetBySpecialtyAsync(specialty, cancellationToken));
    }
}
