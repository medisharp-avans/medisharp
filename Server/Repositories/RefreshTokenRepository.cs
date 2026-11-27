using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task<List<RefreshToken>> GetUnrevokedByUserAsync(int userId)
        {
            return await _dbSet.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        }
    }
}
