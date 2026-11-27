using Server.Models;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;

        public PatientService(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Patient>> GetPatientsAsync()
        {
            return await _repository.GetAllPatientsAsync();
        }
    }
}
