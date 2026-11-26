using Server.Data;
using Server.Models.DTO.Requests;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class ReferralRepository(AppDbContext context) : Repository<Referral>(context), IReferralRepository
    {
        /// <inheritdoc />
        public async Task<Referral> CreateAsync(CreateReferralRequestDTO dto)
        {
            var referral = new Referral
            {
                PatientId = dto.PatientId,
                GeneralPractitionerId = dto.GeneralPractitionerId,
                Specialism = dto.Specialism,
                PreferredSpecialistId = dto.PreferredSpecialistId,
                Reason = dto.Reason,
                Questionnaire = dto.Questionnaire ?? string.Empty,
                Urgency = dto.Urgency,
                TreatmentCode = dto.TreatmentCode,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _context.Referrals.Add(referral);
            await _context.SaveChangesAsync();

            return referral;
        }
    }
}