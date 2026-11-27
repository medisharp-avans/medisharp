
using Server.Models.Enums;

namespace Server.Models
{
    public class Referral
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        public int GeneralPractitionerId { get; set; }

        public GeneralPractitioner GeneralPractitioner { get; set; } = null!;

        public Specialism Specialism { get; set; }

        public int? PreferredSpecialistId { get; set; }

        public Specialist? PreferredSpecialist { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Questionnaire { get; set; } = string.Empty;

        public ReferralUrgency Urgency { get; set; } = ReferralUrgency.Regulier;

        public string TreatmentCode { get; set; } = string.Empty;

        public TreatmentCode TreatmentCodeDetails { get; set; } = null!;

        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Appointment? Appointment { get; set; }
    }
}
