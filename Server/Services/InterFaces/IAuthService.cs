using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Checks the credentials and returns a JWT, or <c>null</c> when the login fails
        /// (unknown user, wrong password or inactive account). The reason is not
        /// returned on purpose, so the caller cannot find out which accounts exist.
        /// </summary>
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);

        /// <summary>Returns the user with the given id, or <c>null</c> when it does not exist or is inactive.</summary>
        Task<AuthUserDTO?> GetCurrentUserAsync(int userId);
    }
}
