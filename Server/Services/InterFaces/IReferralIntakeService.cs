using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    /// <summary>Provides the referral workflow independently of the HTTP endpoints.</summary>
    public interface IReferralIntakeService
    {
        /// <summary>Returns field errors or a confirmation after the referral has been saved.</summary>
        Task<CreateReferralResult> CreateAsync(
            CreateReferralRequest request, CancellationToken cancellationToken = default);

        /// <summary>Saves the referral and validated attachment metadata together.</summary>
        Task<CreateReferralResult> CreateWithAttachmentsAsync(CreateReferralRequest request,
            IReadOnlyList<ReferralAttachment> attachments, CancellationToken cancellationToken);

        /// <summary>Finds the patient details needed to prefill a referral, or returns null.</summary>
        Task<ReferralPatientResponse?> FindPatientByBsnAsync(string bsn, CancellationToken cancellationToken);

        /// <summary>Returns one page of open referrals, starting at page one.</summary>
        Task<IReadOnlyList<OpenReferralResponse>> GetOpenReferralsAsync(int page, CancellationToken cancellationToken);
    }
}
