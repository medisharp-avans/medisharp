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
        private readonly ICalculateService _calculateService;

        public AppointmentService(IAppointmentRepository repository, ICalculateService calculateService)
        {
            _repository = repository;
            _calculateService = calculateService;
        }

        public async Task<List<Appointment>> GetAppointmentAsync()
        {
            return await _repository.GetAsync();
        }

        /// <exception cref="KeyNotFoundException">De behandelcode bestaat niet.</exception>
        /// <exception cref="SlotUnavailableException">De specialist is niet beschikbaar of heeft het tijdslot al.</exception>
        public async Task<Appointment> CreateAppointment(AppointmentManualRequestDTO dto)
        {
            var endTime = await _calculateService.CalculateAppointmentEndTime(dto.StartTime, dto.TreatmentCode);

            if (!await _repository.IsWithinAvailabilityAsync(dto.SpecialistId, dto.StartTime, endTime))
                throw new SlotUnavailableException("De specialist is op dit tijdstip niet beschikbaar.");

            if (await _repository.HasOverlapAsync(dto.SpecialistId, dto.StartTime, endTime))
                throw new SlotUnavailableException("Dit tijdslot is niet meer beschikbaar.");

            // De verwijzing en de afspraak worden samen opgeslagen. Een gelijktijdige boeking van
            // hetzelfde tijdslot wordt door de unique index tegengehouden (SlotUnavailableException).
            return await _repository.CreateWithReferralAsync(
                new CreateAppointmentDTO
                {
                    PatientId = dto.PatientId,
                    SpecialistId = dto.SpecialistId,
                    StartTime = dto.StartTime,
                    EndTime = endTime
                },
                new CreateReferralRequestDTO
                {
                    PatientId = dto.PatientId,
                    GeneralPractitionerId = dto.GeneralPractitionerId,
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