using Server.Models.Enums;

namespace Server.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int SpecialistId { get; set; }
        public Specialist Specialist { get; set; } = null!;
        public int? ReferralId { get; set; }
        public Referral? Referral { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public AppointmentStatus Status { get; set; }
    }
}