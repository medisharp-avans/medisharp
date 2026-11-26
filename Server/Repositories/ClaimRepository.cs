using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class ClaimRepository : Repository<Claim>, IClaimRepository
    {
        public ClaimRepository(AppDbContext context): base(context)
        {
        }

        /// <inheritdoc />
        public async Task<List<Claim>> GetAllWithDetailsAsync()
        {
            return await _context.Claims
                .AsNoTracking()
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .Include(c => c.ClaimLines)
                .ToListAsync();
        }
    }
}
