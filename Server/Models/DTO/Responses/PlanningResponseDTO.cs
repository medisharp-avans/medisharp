namespace Server.Models.DTO.Responses
{
    public class PlanningResponseDTO
    {
        public int patientId { get; set; }
        public string patientFullName { get; set; }

        public int? GeneralPractitionerId { get; set; }
        public string? GeneralPractitionerFullName { get; set; }

        public int? SpecialistId { get; set; }
        public string? SpecialistFullName { get; set; }
    }
}
