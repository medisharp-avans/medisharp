using Server.Models.DTO.Responses;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class PlanningService : IPlanningService
    {
        private readonly IPlanningRepository _repository;

        public PlanningService(IPlanningRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PlanningResponseDTO>> GetPatientsAsync()
        {
            return await _repository.GetAllPatientsAsync();
        }
    }
}
