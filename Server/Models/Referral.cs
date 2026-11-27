using Server.Models.Enums;
namespace Server.Models
{
    /// <summary>
    /// A referral containing only the patient and medical information selected by the GP.
    /// Patient details are a snapshot of what was supplied at the time of referral.
    /// </summary>
    public class Referral
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }

        public required string Bsn { get; set; }
        public required string PatientName { get; set; }
        // Null is possible only for referrals saved before postcode and house number were collected.
        public string? PatientPostalCode { get; set; }
        public string? PatientHouseNumber { get; set; }
        public string? PatientPhoneNumber { get; set; }
        public required string PatientEmailAddress { get; set; }

        public required string Specialty { get; set; }
        public required string TreatmentCode { get; set; }
        public required string ReasonForReferral { get; set; }
        public required string ClinicalQuestion { get; set; }
        public required string Medication { get; set; }
        public required string MedicalInformation { get; set; }
        public required string Allergies { get; set; }
        public required ReferralUrgency Urgency { get; set; }

        public ReferralStatus Status { get; private set; } = ReferralStatus.Open;
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public ICollection<ReferralAttachment> Attachments { get; } = new List<ReferralAttachment>();
    }
}
