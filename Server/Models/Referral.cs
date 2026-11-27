
using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        [Required]
        public int GeneralPractitionerId { get; set; }

        public GeneralPractitioner GeneralPractitioner { get; set; } = null!;

        [Required]
        [EnumDataType(typeof(Specialism))]
        public Specialism Specialism { get; set; } 

        public int? PreferredSpecialistId { get; set; }

        public Specialist? PreferredSpecialist { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Questionnaire { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Urgency { get; set; } = "Regulier";

        public string TreatmentCode { get; set; } = string.Empty;

        public TreatmentCode TreatmentCodeDetails { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Appointment? Appointment { get; set; }
    }
}
