using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    public class CreateReferralRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int PatientId { get; set; }

        [Range(1, int.MaxValue)]
        public int GeneralPractitionerId { get; set; }

        [EnumDataType(typeof(Specialism))]
        public Specialism Specialism { get; set; }

        [Range(1, int.MaxValue)]
        public int? PreferredSpecialistId { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Questionnaire { get; set; }

        [EnumDataType(typeof(ReferralUrgency))]
        public ReferralUrgency Urgency { get; set; } = ReferralUrgency.Regulier;

        [Required]
        [StringLength(20)]
        public string TreatmentCode { get; set; } = string.Empty;
    }
}