using Server.Models.Enums;
namespace Server.Models
{
    /// <summary>
    /// A referral containing only the patient and medical information selected by the GP.
    /// Patient details are a snapshot, independent of any future shared patient record.
    /// </summary>
    public class Referral
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Bsn { get; set; }
        public required string PatientName { get; set; }
        public required string PatientAddress { get; set; }
        public required string PatientCity { get; set; }
        public required string PatientPhoneNumber { get; set; }
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
