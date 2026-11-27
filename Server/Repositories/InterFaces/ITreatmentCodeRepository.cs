using Server.Models;

namespace Server.Repositories.InterFaces
{
    public interface ITreatmentCodeRepository : IRepository<TreatmentCode>
    {
        Task<TreatmentCode?> GetByCodeAsync(string code);
    }
}
