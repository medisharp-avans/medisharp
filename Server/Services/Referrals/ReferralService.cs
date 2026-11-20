using Server.Contracts.Referrals;
using Server.Data.Repositories;
using Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Server.Services.Referrals
{
    /// <summary>Creates a patient snapshot after validating the referral's required fields.</summary>
    public class ReferralService(IReferralRepository repository) : IReferralService
    {
        /// <inheritdoc />
        public async Task<CreateReferralResult> CreateAsync(
            CreateReferralRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults,
                    validateAllProperties: true))
            {
                return new CreateReferralResult
                {
                    Errors = validationResults
                        .SelectMany(result => result.MemberNames.Select(member => new
                        {
                            Field = JsonNamingPolicy.CamelCase.ConvertName(member),
                            Message = result.ErrorMessage!
                        }))
                        .GroupBy(error => error.Field)
                        .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray())
                };
            }

            var referral = new Referral
            {
                Bsn = request.Bsn!,
                PatientName = request.PatientName!,
                PatientAddress = request.PatientAddress!,
                PatientCity = request.PatientCity!,
                PatientPhoneNumber = request.PatientPhoneNumber!,
                PatientEmailAddress = request.PatientEmailAddress!,
                Specialty = request.Specialty!,
                TreatmentCode = request.TreatmentCode!,
                ReasonForReferral = request.ReasonForReferral!,
                ClinicalQuestion = request.ClinicalQuestion!,
                Medication = request.Medication!,
                MedicalInformation = request.MedicalInformation!,
                Allergies = request.Allergies!,
                Urgency = request.Urgency!.Value
            };

            await repository.AddAsync(referral, cancellationToken);

            return new CreateReferralResult
            {
                Referral = new CreatedReferralResponse
                {
                    Id = referral.Id,
                    Status = referral.Status.ToString(),
                    CreatedAtUtc = referral.CreatedAtUtc
                }
            };
        }
    }
}
