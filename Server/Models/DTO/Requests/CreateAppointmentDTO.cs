namespace Server.Models.DTO.Requests
{
    public class CreateAppointmentDTO
    {
        public int PatientId { get; set; }
        public int SpecialistId { get; set; }
        public int ReferralId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
