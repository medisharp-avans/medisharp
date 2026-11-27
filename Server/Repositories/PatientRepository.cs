using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients.Select(p => new Patient
            {
                Id = p.Id,
                User = new User
                {
                    FullName = p.User.FullName
                }
            }).ToListAsync();
        }
    }
}
