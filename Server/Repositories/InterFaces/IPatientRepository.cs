using Server.Models;

namespace Server.Repositories.InterFaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task<List<Patient>> GetAllPatientsAsync();
    }
}
