using Server.Models;
using Server.Models.DTO;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    public interface IPlanningService
    {
        Task<List<PlanningResponseDTO>> GetPatientsAsync(); 
        Task<List<TreatmentCode>> GetCodesBySpecialist(string spec);
        Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec);
        Task<SpecialistAppointmentDTO> SetSpecialistForAppointment(AppointmentManualRequestDTO dto);
    }
}
