using Server.Models.Enums;
using Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Server.Models.DTO.Requests
{
    /// <summary>Contains only the patient and medical fields selected for a new referral.</summary>
    /// <remarks>Server-generated values, full dossiers and attachment storage keys are not accepted.</remarks>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateReferralRequest
    {
        [Required(ErrorMessage = "Vul het BSN in")]
        [StringLength(9, ErrorMessage = "Het BSN mag maximaal 9 tekens bevatten")]
        public string? Bsn { get; set; }

        [Required(ErrorMessage = "Vul de naam in")]
        [StringLength(200, ErrorMessage = "De naam mag maximaal 200 tekens bevatten")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Vul het adres in")]
        [StringLength(300, ErrorMessage = "Het adres mag maximaal 300 tekens bevatten")]
        public string? PatientAddress { get; set; }

        [Required(ErrorMessage = "Vul de woonplaats in")]
        [StringLength(100, ErrorMessage = "De woonplaats mag maximaal 100 tekens bevatten")]
        public string? PatientCity { get; set; }

        [Required(ErrorMessage = "Vul het telefoonnummer in")]
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

        [Required(ErrorMessage = "Vul de vraagstelling in")]
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
    }
}
