using Server.Models;
using Server.Models.DTO.Responses;

namespace Server.Repositories.InterFaces
{
    public interface IPlanningRepository : IRepository<Patient>
    {
        Task<List<PlanningResponseDTO>> GetAllPatientsAsync();
        Task<List<TreatmentCode>> GetCodesBySpecialism(string spec);
        Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec);

        /// <summary>Heeft de specialist een niet-geannuleerde afspraak die overlapt met [start, end)?</summary>
        Task<bool> HasOverlapAsync(int specialistId, DateTime start, DateTime end);

        /// <summary>Valt [start, end] helemaal binnen één beschikbaarheidsblok van de specialist?</summary>
        Task<bool> IsWithinAvailabilityAsync(int specialistId, DateTime start, DateTime end);
    }
}
