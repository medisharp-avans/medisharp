using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    /// <summary>Validates and creates referrals independently of the HTTP endpoint.</summary>
    public interface IReferralService
    {
        /// <summary>Returns field errors or a confirmation after the referral has been saved.</summary>
        Task<CreateReferralResult> CreateAsync(
            CreateReferralRequest request, CancellationToken cancellationToken = default);
    }
}
