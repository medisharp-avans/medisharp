using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    public interface IAuthService
    {
        /// Checks the credentials and emails a verification code. Returns <c>null</c> when the login fails
        /// (unknown user, wrong password or inactive account). The reason is not returned on purpose, so the caller cannot find out which accounts exist.
        Task<LoginChallengeDTO?> LoginAsync(LoginRequestDTO request);

        /// Checks the emailed code and returns a JWT, or <c>null</c> when the code is wrong, expired or tried too often.
        Task<LoginResponseDTO?> VerifyLoginAsync(VerifyLoginRequestDTO request);

        /// <summary>Returns the user with the given id, or <c>null</c> when it does not exist or is inactive.</summary>
        Task<AuthUserDTO?> GetCurrentUserAsync(int userId);
    }
}
