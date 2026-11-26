using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class TreatmentCodeRepository(AppDbContext context) : Repository<TreatmentCode>(context), ITreatmentCodeRepository
    {
        /// <inheritdoc />
        public async Task<TreatmentCode?> GetByCodeAsync(string code)
        {
            return await _context.TreatmentCodes.FirstOrDefaultAsync(t => t.Code == code);
        }
    }
}
