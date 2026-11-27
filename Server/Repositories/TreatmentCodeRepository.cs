using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class TreatmentCodeRepository : Repository<TreatmentCode>, ITreatmentCodeRepository
    {
        public TreatmentCodeRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<TreatmentCode?> GetByCodeAsync(string code)
        {
            return await _context.TreatmentCodes.FirstOrDefaultAsync(t => t.Code == code);
        }
    }
}
