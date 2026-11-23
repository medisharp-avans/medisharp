using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Services.InterFaces;

namespace Server.Controllers
{
    /// <summary>Login and information about the logged in user.</summary>
    [ApiController]
    [Route("api/")]
    public class AuthenticationController(IAuthService authService) : ControllerBase
    {
        /// <summary>Logs in with a username and a password.</summary>
        /// <response code="200">Login succeeded; the response contains the JWT.</response>
        /// <response code="400">The request is invalid (missing fields).</response>
        /// <response code="401">Wrong credentials, or the account is inactive.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType<LoginResponseDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequestDTO request)
        {
            var result = await authService.LoginAsync(request);
            if (result is null)
                return Unauthorized(new { message = "Ongeldige inloggegevens." });

            return Ok(result);
        }

        /// <summary>Returns the user that belongs to the JWT in the Authorization header.</summary>
        /// <response code="200">The logged in user.</response>
        /// <response code="401">No valid token, or the user no longer exists or is inactive.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType<AuthUserDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var user = await authService.GetCurrentUserAsync(userId);
            return user is null ? Unauthorized() : Ok(user);
        }
    }
}
