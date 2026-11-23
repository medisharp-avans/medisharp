using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    public interface IPlanningService
    {
        Task<List<PlanningResponseDTO>> GetPatientsAsync(); 
    }
}
