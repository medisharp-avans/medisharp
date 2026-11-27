using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    /// <summary>
    /// Antwoord van POST /api/appointments. Een DTO, want de Appointment-entity verwijst naar de Referral
    /// en die weer naar de Appointment, en die lus kan niet naar JSON.
    /// </summary>
    public record AppointmentResponseDTO(
        int Id,
        int PatientId,
        int SpecialistId,
        int? ReferralId,
        DateTime StartTime,
        DateTime EndTime,
        AppointmentStatus Status);
}