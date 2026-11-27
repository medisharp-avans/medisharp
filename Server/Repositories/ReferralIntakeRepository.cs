using Referral = Server.Models.Intake.Referral;
using Server.Data;
using Server.Repositories.InterFaces;
using Server.Models.Enums;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Microsoft.EntityFrameworkCore;

namespace Server.Repositories
{
    /// <summary>Reads and persists referrals and intake patients through a scoped EF Core context.</summary>
    public class ReferralIntakeRepository(ReferralDbContext context) : IReferralIntakeRepository
    {
        /// <inheritdoc />
        public Task<ReferralPatientResponse?> FindPatientByBsnAsync(string bsn, CancellationToken cancellationToken) =>
            context.Patients.AsNoTracking()
                .Where(patient => patient.Bsn == bsn)
                .Select(patient => new ReferralPatientResponse(patient.FullName, patient.PostalCode,
                    patient.HouseNumber, patient.PhoneNumber, patient.Email))
                .SingleOrDefaultAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<OpenReferralResponse>> GetOpenPageAsync(int page, CancellationToken cancellationToken)
        {
            return await context.Referrals.AsNoTracking()
                .Where(referral => referral.Status == ReferralStatus.Open)
                .OrderBy(referral => referral.CreatedAtUtc)
                .ThenBy(referral => referral.Id)
                .Skip((page - 1) * OpenReferralsRequest.PageSize)
                .Take(OpenReferralsRequest.PageSize)
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

            var newPatient = new Models.Intake.Patient
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
                // EF saves the patient and referral in one transaction.
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when
                (exception.Entries.Any(entry => ReferenceEquals(entry.Entity, newPatient)))
            {
                // Another request may have inserted this BSN while this request was saving.
                // Retry only if that patient exists. Otherwise propagate the original failure.
                var concurrentlyCreatedPatient = await context.Patients
                    .SingleOrDefaultAsync(patient => patient.Bsn == referral.Bsn, cancellationToken);
                if (concurrentlyCreatedPatient is null) throw;

                context.Entry(newPatient).State = EntityState.Detached;
                context.Entry(referral).Reference(entity => entity.Patient).CurrentValue = concurrentlyCreatedPatient;
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private Task<int?> FindPatientIdAsync(string bsn, CancellationToken cancellationToken) =>
            context.Patients.Where(patient => patient.Bsn == bsn)
                .Select(patient => (int?)patient.Id)
                .SingleOrDefaultAsync(cancellationToken);
    }
}
