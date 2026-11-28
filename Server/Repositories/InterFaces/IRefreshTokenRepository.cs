using Server.Models;

namespace Server.Repositories.InterFaces
{
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash);

        /// <summary>All tokens of the user that are not revoked yet (expired ones included).</summary>
        Task<List<RefreshToken>> GetUnrevokedByUserAsync(int userId);
    }
}
