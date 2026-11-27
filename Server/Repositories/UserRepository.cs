using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
    {
        public async Task<User?> GetByEmailAsync(string email)
        {
            // SQL Server vergelijkt standaard hoofdletterongevoelig.
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
