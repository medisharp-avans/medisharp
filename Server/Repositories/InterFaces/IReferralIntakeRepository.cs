using Referral = Server.Models.Intake.Referral;
using Server.Models.DTO.Responses;

namespace Server.Repositories.InterFaces
{
    /// <summary>Reads and writes the referral workflow data without exposing the database context.</summary>
    public interface IReferralIntakeRepository
    {
        /// <summary>Saves a new referral and completes only after the database operation succeeds.</summary>
        Task AddAsync(Referral referral, CancellationToken cancellationToken);

        /// <summary>Finds the contact details for an exact BSN, or returns null.</summary>
        Task<ReferralPatientResponse?> FindPatientByBsnAsync(string bsn, CancellationToken cancellationToken);

        /// <summary>Returns a page of open referrals in creation order.</summary>
        Task<IReadOnlyList<OpenReferralResponse>> GetOpenPageAsync(int page, CancellationToken cancellationToken);
    }
}
