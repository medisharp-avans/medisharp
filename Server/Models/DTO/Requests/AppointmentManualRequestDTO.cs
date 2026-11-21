
using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    public class AppointmentManualRequestDTO
    {
        // Appointment gegevens
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int SpecialistId { get; set; }

        [Required]
        public DateTime StartTime {  get; set; }

        public int? ReferralId { get; set; }

        [Required]
        public int GeneralPractitionerId { get; set; }

        [Required]
        [EnumDataType(typeof(Specialism))]
        public Specialism Specialism { get; set; } 

        public int? PreferredSpecialistId { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Questionnaire { get; set; }

        [Required]
        [StringLength(20)]
        public string Urgency { get; set; } = "Regulier";

        [Required]
        [StringLength(20)]
        public string TreatmentCode { get; set; } = string.Empty;
    }
}
