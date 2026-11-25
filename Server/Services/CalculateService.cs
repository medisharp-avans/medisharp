
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class CalculateService : ICalculateService
    {
        private readonly ITreatmentCodeRepository _repository;

        public CalculateService(ITreatmentCodeRepository repository)
        {
            _repository = repository;
        }

        public async Task<DateTime> CalculateAppointmentEndTime(
            DateTime startTime,
            string treatmentCode)
        {
            var code = await _repository.GetByCodeAsync(treatmentCode);

            if (code == null)
            {
                throw new KeyNotFoundException(
                    $"Zorgcode {treatmentCode} bestaat niet.");
            }

            var durationInMinutes = code.DurationInQuarters * 15;

            return startTime.AddMinutes(durationInMinutes);
        }


    }
}
