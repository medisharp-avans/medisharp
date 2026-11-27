using Referral = Server.Models.Intake.Referral;
using Server.Data;
using Server.Repositories.InterFaces;
using Server.Models;
using Server.Models.Enums;
using Server.Models.DTO.Responses;
using Microsoft.EntityFrameworkCore;

namespace Server.Repositories
{
    /// <summary>Persists referrals through the request's scoped EF Core context.</summary>
    public class ReferralIntakeRepository(ReferralDbContext context) : IReferralIntakeRepository
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
            var existingId = await FindPatientIdAsync(referral.Bsn, cancellationToken);
            if (existingId is not null)
            {
                referral.PatientId = existingId;
                context.Referrals.Add(referral);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            var newPatient = new Server.Models.Intake.Patient
            {
                Bsn = referral.Bsn,
                FullName = referral.PatientName,
                PostalCode = referral.PatientPostalCode!,
                HouseNumber = referral.PatientHouseNumber!,
                PhoneNumber = referral.PatientPhoneNumber,
                Email = referral.PatientEmailAddress
            };
            referral.Patient = newPatient;
            context.Referrals.Add(referral);
            try
            {
                // EF saves both inserts together. The unique BSN index resolves concurrent registration.
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when
                (exception.Entries.Any(entry => ReferenceEquals(entry.Entity, newPatient)))
            {
                var concurrentlyCreatedId = await FindPatientIdAsync(referral.Bsn, cancellationToken);
                if (concurrentlyCreatedId is null) throw;

                referral.Patient = null;
                context.Entry(newPatient).State = EntityState.Detached;
                referral.PatientId = concurrentlyCreatedId;
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private Task<int?> FindPatientIdAsync(string bsn, CancellationToken cancellationToken) =>
            context.Patients.Where(patient => patient.Bsn == bsn)
                .Select(patient => (int?)patient.Id)
                .SingleOrDefaultAsync(cancellationToken);
    }
}
