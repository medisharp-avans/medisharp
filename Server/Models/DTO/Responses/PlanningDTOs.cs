using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    public record PatientDetailDTO(int Id, string Name, int? GeneralPractitionerId);

    public record ReferralListItemDTO(int Id, string PatientName, Specialism Specialism, string Urgency, string Status);

    public record ReferralDetailDTO(
        int Id,
        int PatientId,
        string PatientName,
        int GeneralPractitionerId,
        Specialism Specialism,
        int? PreferredSpecialistId,
        string Reason,
        string? Questionnaire,
        string Urgency,
        string TreatmentCode,
        string Status);

    public record TreatmentCodeDTO(string Code, string Description, int DurationInQuarters);

    public record TimeSlotDTO(int SpecialistId, string SpecialistName, DateTime StartTime);
}