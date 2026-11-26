using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalliDesk.Authorization;

namespace RevalliDesk.Controllers
{
    /// <summary>
    /// Base class for every controller in RevaliDesk.
    /// The [Authorize] attribute here means: every endpoint needs a logged-in user (any role),
    /// unless the endpoint itself says [AllowAnonymous]. A new controller is therefore closed by default.
    /// Which role may use a controller is set on the controller itself, for example:
    /// [Authorize(Roles = nameof(UserRole.GeneralPractitioner))] on PatientsController,
    /// [Authorize(Roles = nameof(UserRole.SystemAdministrator))] on AuditLogController.
    /// Class and method attributes are combined with AND, so the role cannot be set here as well.
    /// </summary>
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    // GlobalExceptionHandler can answer any endpoint with a 500, so every endpoint documents it.
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>The ID of the logged-in user. Only call this in endpoints that require a login.</summary>
        protected int CurrentUserId => CurrentUser.GetId(User)
            ?? throw new InvalidOperationException("No logged-in user. Is this endpoint [AllowAnonymous]?");

        /// <summary>403 Forbidden for a patient of another GP.</summary>
        protected IActionResult PatientForbidden() => Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Geen toegang",
            detail: "U heeft geen toegang tot deze patiënt.");
    }
}