using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    public class PlanningResponseDTO
    {
        public int? patientId { get; set; }
        public string? patientFullName { get; set; }
        public int? SpecialistId { get; set; }
        public string? SpecialistFullName { get; set; }
        public string? Specialism { get; set; }
    }
}
