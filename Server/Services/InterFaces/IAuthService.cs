using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    public interface IAuthService
    {
        /// Checks the credentials and emails a verification code. Returns <c>null</c> when the login fails
        /// (unknown user, wrong password or inactive account). The reason is not returned on purpose, so the caller cannot find out which accounts exist.
        Task<LoginChallengeDTO?> LoginAsync(LoginRequestDTO request);

        /// Checks the emailed code and returns a JWT and a refresh token, or <c>null</c> when the code is wrong, expired or tried too often.
        Task<LoginResponseDTO?> VerifyLoginAsync(VerifyLoginRequestDTO request);

        /// Swaps a refresh token for a new JWT and a new refresh token (the old one stops working).
        /// Returns <c>null</c> when the token is unknown, expired, already used, or the user is inactive.
        /// Presenting an already used token ends all sessions of that user.
        Task<LoginResponseDTO?> RefreshAsync(RefreshTokenRequestDTO request);

        /// Revokes the refresh token, so the session cannot be extended any more. Unknown tokens are ignored.
        Task LogoutAsync(RefreshTokenRequestDTO request);

        /// <summary>Returns the user with the given id, or <c>null</c> when it does not exist or is inactive.</summary>
        Task<AuthUserDTO?> GetCurrentUserAsync(int userId);
    }
}
