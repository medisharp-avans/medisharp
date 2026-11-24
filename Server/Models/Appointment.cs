
using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        [Required]
        public int SpecialistId { get; set; }

        public Specialist Specialist { get; set; } = null!;

        public int? ReferralId { get; set; }

        public Referral? Referral { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [EnumDataType(typeof(AppointmentStatus))]
        public AppointmentStatus Status { get; set; }
    }
}
