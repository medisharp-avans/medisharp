using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.DTO.Responses;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class PlanningRepository : Repository<Patient>, IPlanningRepository
    {
        public PlanningRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<List<PlanningResponseDTO>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Select(p => new PlanningResponseDTO
                {
                    patientId = p.Id,
                    patientFullName = p.User.FullName,
                    GeneralPractitionerId = p.GeneralPractitionerId,
                    GeneralPractitionerFullName = p.GeneralPractitioner.User.FullName,
                })
                .ToListAsync();
        }
    }
}
