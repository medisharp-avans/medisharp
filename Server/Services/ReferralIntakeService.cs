using Referral = Server.Models.Intake.Referral;
using Server.Services.InterFaces;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Server.Services
{
    /// <summary>Creates a patient snapshot after validating the referral's required fields.</summary>
    public class ReferralIntakeService(IReferralIntakeRepository repository, ITreatmentCatalogRepository treatmentCatalog)
        : IReferralIntakeService
    {
        /// <inheritdoc />
        public async Task<CreateReferralResult> CreateAsync(
            CreateReferralRequest request, CancellationToken cancellationToken = default)
            => await CreateWithAttachmentsAsync(request, [], cancellationToken);

        /// <summary>Persists validated attachment metadata together with the referral in one database save.</summary>
        public async Task<CreateReferralResult> CreateWithAttachmentsAsync(
            CreateReferralRequest request, IReadOnlyList<ReferralAttachment> attachments,
            CancellationToken cancellationToken = default)
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

            var treatment = await treatmentCatalog.FindByCodeAsync(
                request.TreatmentCode!.Trim().ToUpperInvariant(), cancellationToken);
            if (treatment is null)
            {
                return new CreateReferralResult
                {
                    Errors = new() { ["treatmentCode"] = ["Kies een geldige behandelcode"] }
                };
            }

            if (!string.Equals(treatment.Specialty, request.Specialty!.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return new CreateReferralResult
                {
                    Errors = new() { ["treatmentCode"] = ["De behandelcode hoort niet bij het gekozen specialisme"] }
                };
            }

            var referral = new Referral
            {
                Bsn = request.Bsn!,
                PatientName = request.PatientName!,
                PatientPostalCode = request.PatientPostalCode!.Trim(),
                PatientHouseNumber = request.PatientHouseNumber!.Trim(),
                PatientPhoneNumber = string.IsNullOrWhiteSpace(request.PatientPhoneNumber)
                    ? null
                    : request.PatientPhoneNumber.Trim(),
                PatientEmailAddress = request.PatientEmailAddress!,
                Specialty = treatment.Specialty,
                TreatmentCode = treatment.Code,
                ReasonForReferral = request.ReasonForReferral!,
                ClinicalQuestion = request.ClinicalQuestion ?? string.Empty,
                Medication = request.Medication!,
                MedicalInformation = request.MedicalInformation!,
                Allergies = request.Allergies!,
                Urgency = request.Urgency!.Value
            };

            foreach (var attachment in attachments) referral.Attachments.Add(attachment);
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
