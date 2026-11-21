using Server.Models;
using Server.Models.DTO.Requests;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IReferralService _referralService;
        private readonly ICalculateService _calculateService;

        public AppointmentService(IAppointmentRepository repository, IReferralService referralService, ICalculateService calculateService)
        {
            _repository = repository;
            _referralService = referralService;
            _calculateService = calculateService;
        }

        public async Task<List<Appointment>> GetAppointmentAsync()
        {
            return await _repository.GetAsync();
        }

        public async Task<Appointment> CreateAppointment(
            AppointmentManualRequestDTO dto)
        {
            var endTime = await _calculateService.CalculateAppointmentEndTime(dto.StartTime, dto.TreatmentCode);

            var referral = await _referralService.CreateAsync(
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

            return await _repository.CreateAsync(
                new CreateAppointmentDTO
                {
                    PatientId = dto.PatientId,
                    SpecialistId = dto.SpecialistId,
                    StartTime = dto.StartTime,
                    EndTime = endTime,
                    ReferralId = referral.Id
                });
        }
    }
}
