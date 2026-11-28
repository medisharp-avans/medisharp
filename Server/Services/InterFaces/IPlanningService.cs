using Server.Models;
using Server.Models.DTO;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    /// <summary>
    /// Provides business logic operations for planning, schedules, and specialist assignments.
    /// </summary>
    public interface IPlanningService
    {
        /// <summary>
        /// Retrieves a list of all patients formatted for planning purposes asynchronously.
        /// </summary>
        /// <returns>A list of <see cref="PlanningResponseDTO"/> containing patient planning details.</returns>
        Task<List<PlanningResponseDTO>> GetPatientsAsync();

        /// <summary>
        /// Retrieves treatment codes associated with a specific specialist.
        /// </summary>
        /// <param name="spec">The identifier or name of the specialist.</param>
        /// <returns>A list of matching <see cref="TreatmentCode"/> entities.</returns>
        Task<List<TreatmentCode>> GetCodesBySpecialist(string spec);

        /// <summary>
        /// Retrieves specialists associated with a specific medical specialism, formatted as planning response data transfer objects.
        /// </summary>
        /// <param name="spec">The name or code of the specialism.</param>
        /// <returns>A list of <see cref="PlanningResponseDTO"/> representing the specialists.</returns>
        Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec);

        /// <summary>
        /// Assigns a specialist to an appointment based on manual request details.
        /// </summary>
        /// <param name="dto">The data transfer object containing the appointment and specialist assignment details.</param>
        /// <returns>A <see cref="SpecialistAppointmentDTO"/> containing the updated appointment and specialist info.</returns>
        Task<SpecialistAppointmentDTO> SetSpecialistForAppointment(AppointmentManualRequestDTO dto);
    }
}