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
        /// <summary>Step 1: checks email address and password and emails a verification code (PRI-04).</summary>
        /// <response code="200">Credentials correct; a code was sent. Continue with <c>POST /api/login/verify</c>.</response>
        /// <response code="400">The request is invalid (missing fields).</response>
        /// <response code="401">Wrong credentials, or the account is inactive.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType<LoginChallengeDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequestDTO request)
        {
            var result = await authService.LoginAsync(request);
            if (result is null)
                return Unauthorized(new { message = "Ongeldige inloggegevens." });

            return Ok(result);
        }

        /// <summary>Step 2: checks the emailed verification code and returns the JWT.</summary>
        /// <response code="200">Code correct; the response contains the JWT.</response>
        /// <response code="400">The request is invalid (code is not 6 digits).</response>
        /// <response code="401">Wrong or expired code, or too many attempts.</response>
        [HttpPost("login/verify")]
        [AllowAnonymous]
        [ProducesResponseType<LoginResponseDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyLogin(VerifyLoginRequestDTO request)
        {
            var result = await authService.VerifyLoginAsync(request);
            if (result is null)
                return Unauthorized(new { message = "Ongeldige of verlopen verificatiecode." });

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
