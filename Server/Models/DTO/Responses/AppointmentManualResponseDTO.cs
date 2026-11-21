using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    public class AppointmentManualResponseDTO
    {
        public class AppointmentResponseDto
        {
            public int Id { get; set; }

            public int PatientId { get; set; }

            public int SpecialistId { get; set; }

            public DateTime StartTime { get; set; }

            public DateTime EndTime { get; set; }

            public string Specialism { get; set; } = string.Empty;

            public AppointmentStatus Status { get; set; }

            public int? ReferralId { get; set; }
        }
    }
}
