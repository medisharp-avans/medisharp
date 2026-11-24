using Microsoft.AspNetCore.Http.HttpResults;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class PlanningService : IPlanningService
    {
        private readonly IPlanningRepository _repository;
        private readonly ICalculateService _calculateService;

        public PlanningService(IPlanningRepository repository, ICalculateService calculateService)
        {
            _repository = repository;
            _calculateService = calculateService;
        }

        /// <inheritdoc />
        public async Task<List<PlanningResponseDTO>> GetPatientsAsync()
        {
            return await _repository.GetAllPatientsAsync();
        }

        /// <inheritdoc />
        public async Task<List<TreatmentCode>> GetCodesBySpecialist(string spec)
        {
            return await _repository.GetCodesBySpecialism(spec);
        }

        /// <inheritdoc />
        public async Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec)
        {
            return await _repository.GetSpecialistsBySpecialism(spec);
        }

        /// <inheritdoc />
        /// <exception cref="SlotUnavailableException">Thrown when no suitable specialist is found who is available within the requested time slot.</exception>
        public async Task<SpecialistAppointmentDTO> SetSpecialistForAppointment(AppointmentManualRequestDTO dto)
        {
            // TODO: Availability is currently tracked via a database table. 
            // The exact scheduling logic is still under development, which is why start times are temporarily hardcoded.
            DateTime startTime = DateTime.Today.AddDays(1).AddHours(10);
            DateTime endTime = await _calculateService.CalculateAppointmentEndTime(startTime, dto.TreatmentCode);

            // First, check if the preferred specialist is specified and meets the availability criteria without scheduling conflicts.
            if (dto.PreferredSpecialistId != null &&
                await _repository.IsWithinAvailabilityAsync(dto.PreferredSpecialistId.Value, startTime, endTime) &&
                !await _repository.HasOverlapAsync(dto.PreferredSpecialistId.Value, startTime, endTime))
            {
                return new SpecialistAppointmentDTO
                {
                    SpecialistId = dto.PreferredSpecialistId.Value,
                    StartTime = startTime,
                    EndTime = endTime
                };
            }

            // If no preferred specialist is available, iterate through all specialists matching the given specialism.
            var specialists = await _repository.GetSpecialistsBySpecialism(dto.Specialism.ToString());

            foreach (var specialist in specialists)
            {
                if (specialist.SpecialistId == null)
                    continue;

                if (await _repository.IsWithinAvailabilityAsync(specialist.SpecialistId.Value, startTime, endTime) &&
                    !await _repository.HasOverlapAsync(specialist.SpecialistId.Value, startTime, endTime))
                {
                    return new SpecialistAppointmentDTO
                    {
                        SpecialistId = specialist.SpecialistId.Value,
                        StartTime = startTime,
                        EndTime = endTime
                    };
                }
            }

            throw new SlotUnavailableException("Geen beschikbare specialist gevonden.");
        }
    }
}
