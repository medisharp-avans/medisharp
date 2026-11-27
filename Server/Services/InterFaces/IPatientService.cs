using Server.Models;

namespace Server.Services.InterFaces
{
    public interface IPatientService
    {
        Task<List<Patient>> GetPatientsAsync(); 
    }
}
