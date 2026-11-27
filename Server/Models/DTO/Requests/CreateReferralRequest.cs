using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Server.Models.DTO.Requests
{
    /// <summary>Contains only the patient and medical fields selected for a new referral.</summary>
    /// <remarks>Server-generated values, full dossiers and attachment storage keys are not accepted.</remarks>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateReferralRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Vul het BSN in")]
        [StringLength(9, ErrorMessage = "Het BSN mag maximaal 9 tekens bevatten")]
        public string? Bsn { get; set; }

        [Required(ErrorMessage = "Vul de naam in")]
        [StringLength(200, ErrorMessage = "De naam mag maximaal 200 tekens bevatten")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Vul de postcode in")]
        [StringLength(12, ErrorMessage = "De postcode mag maximaal 12 tekens bevatten")]
        [RegularExpression(@"^\s*[0-9]{4} ?[A-Za-z]{2}\s*$",
            ErrorMessage = "Vul een postcode in met 4 cijfers en 2 letters, bijvoorbeeld 1234 AB.")]
        public string? PatientPostalCode { get; set; }

        [Required(ErrorMessage = "Vul het huisnummer in")]
        [StringLength(20, ErrorMessage = "Het huisnummer mag maximaal 20 tekens bevatten")]
        public string? PatientHouseNumber { get; set; }

        [StringLength(32, ErrorMessage = "Het telefoonnummer mag maximaal 32 tekens bevatten")]
        public string? PatientPhoneNumber { get; set; }

        [Required(ErrorMessage = "Vul het e-mailadres in")]
        [StringLength(254, ErrorMessage = "Het e-mailadres mag maximaal 254 tekens bevatten")]
        public string? PatientEmailAddress { get; set; }

        [Required(ErrorMessage = "Kies een specialisme")]
        [StringLength(100, ErrorMessage = "Het specialisme mag maximaal 100 tekens bevatten")]
        public string? Specialty { get; set; }

        [Required(ErrorMessage = "Kies een behandelcode")]
        [StringLength(50, ErrorMessage = "De behandelcode mag maximaal 50 tekens bevatten")]
        public string? TreatmentCode { get; set; }

        [Required(ErrorMessage = "Vul de reden van verwijzing in")]
        public string? ReasonForReferral { get; set; }

        /// <summary>Optional separate question from older clients. The current form sends both in ReasonForReferral.</summary>
        public string? ClinicalQuestion { get; set; }

        [Required(ErrorMessage = "Vul de medicatie in, of geef aan dat er geen medicatie is")]
        public string? Medication { get; set; }

        [Required(ErrorMessage = "Vul de relevante medische gegevens in")]
        public string? MedicalInformation { get; set; }

        [Required(ErrorMessage = "Vul de allergieën in, of geef aan dat er geen allergieën bekend zijn")]
        public string? Allergies { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<ReferralUrgency>))]
        [Required(ErrorMessage = "Kies een urgentie")]
        [EnumDataType(typeof(ReferralUrgency), ErrorMessage = "Kies een geldige urgentie")]
        public ReferralUrgency? Urgency { get; set; }

        /// <summary>Checks the exact BSN format after the required fields are validated.</summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Bsn is not { Length: 9 } || !Bsn.All(char.IsAsciiDigit))
                yield return new ValidationResult("Vul een BSN van 9 cijfers in", [nameof(Bsn)]);
        }
    }
}
