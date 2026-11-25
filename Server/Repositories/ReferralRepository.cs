using Server.Data;
using Server.Repositories.InterFaces;
using Server.Models;
using Server.Models.Enums;
using Server.Models.DTO.Responses;
using Microsoft.EntityFrameworkCore;

namespace Server.Repositories
{
    /// <summary>Persists referrals through the request's scoped EF Core context.</summary>
    public class ReferralRepository(AppDbContext context) : IReferralRepository
    {
        public const int OpenPageSize = 25;

        public async Task<IReadOnlyList<OpenReferralResponse>> GetOpenPageAsync(
            int page, CancellationToken cancellationToken)
        {
            return await context.Referrals.AsNoTracking()
                .Where(referral => referral.Status == ReferralStatus.Open)
                .OrderBy(referral => referral.CreatedAtUtc)
                .ThenBy(referral => referral.Id)
                .Skip((page - 1) * OpenPageSize)
                .Take(OpenPageSize)
                .Select(referral => new OpenReferralResponse(referral.Specialty, referral.TreatmentCode,
                    referral.PatientName, referral.Bsn))
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task AddAsync(Referral referral, CancellationToken cancellationToken)
        {
            context.Referrals.Add(referral);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
