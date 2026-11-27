using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Services.InterFaces
{
    public interface IReferralService
    {
        Task<Referral> CreateAsync(CreateReferralRequestDTO dto);
    }
}
