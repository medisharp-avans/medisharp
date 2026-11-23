using Server.Models;
using Server.Models.DTO.Responses;

namespace Server.Repositories.InterFaces
{
    public interface IPlanningRepository : IRepository<Patient>
    {
        Task<List<PlanningResponseDTO>> GetAllPatientsAsync();
    }
}
