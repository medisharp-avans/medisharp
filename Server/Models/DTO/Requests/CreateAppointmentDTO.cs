using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    public class CreateAppointmentDTO
    {
        [Range(1, int.MaxValue)]
        public int PatientId { get; set; }

        [Range(1, int.MaxValue)]
        public int SpecialistId { get; set; }

        [Range(1, int.MaxValue)]
        public int ReferralId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }
}