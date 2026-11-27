using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Repositories.InterFaces
{
    public interface IReferralRepository : IRepository<Referral>
    {
        Task<Referral> CreateAsync(CreateReferralRequestDTO dto);
    }
}
