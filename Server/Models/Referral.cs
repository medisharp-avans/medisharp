
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
        [StringLength(50)]
        public string Specialism { get; set; } = string.Empty;

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

        [Required]
        [StringLength(20)]
        public string TreatmentCode { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Appointment? Appointment { get; set; }
    }
}
