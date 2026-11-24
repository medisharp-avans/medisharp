using Microsoft.AspNetCore.Http.HttpResults;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IPlanningRepository _planningRepository;
        private readonly IPlanningService _planningService;

        public AppointmentService(IAppointmentRepository repository, IPlanningService planningService, IPlanningRepository planningRepository)
        {
            _repository = repository;
            _planningService = planningService;
            _planningRepository = planningRepository;
        }

        public async Task<List<Appointment>> GetAppointmentAsync()
        {
            return await _repository.GetAsync();
        }

        /// <exception cref="KeyNotFoundException">De behandelcode bestaat niet.</exception>
        /// <exception cref="SlotUnavailableException">De specialist is niet beschikbaar of heeft het tijdslot al.</exception>
        public async Task<Appointment> CreateAppointment(AppointmentManualRequestDTO dto)
        {
            var newDto = await _planningService.SetSpecialistForAppointment(dto);
            var patient = await _planningRepository.GetByIdAsync(dto.PatientId);

            if (patient == null)
                throw new KeyNotFoundException("De patiënt bestaat niet.");

            return await _repository.CreateWithReferralAsync(
                new CreateAppointmentDTO
                {
                    PatientId = dto.PatientId,
                    SpecialistId = newDto.SpecialistId,
                    StartTime = newDto.StartTime,
                    EndTime = newDto.EndTime
                },
                new CreateReferralRequestDTO
                {
                    PatientId = dto.PatientId,
                    GeneralPractitionerId = patient.GeneralPractitionerId ?? 0,
                    Specialism = dto.Specialism,
                    PreferredSpecialistId = dto.PreferredSpecialistId,
                    Reason = dto.Reason,
                    Questionnaire = dto.Questionnaire,
                    Urgency = dto.Urgency,
                    TreatmentCode = dto.TreatmentCode
                });
        }
    }
}