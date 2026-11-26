using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    /// <summary>
    /// One row of the claims overview (GET /api/claims). A DTO, because the Claim entity points to the
    /// patient and the claim lines, and we only want to send what the overview shows.
    /// </summary>
    public record ClaimOverviewDTO(
        int Id,
        int PatientId,
        string PatientName,
        string PolicyNumber,
        ClaimStatus Status,
        int LineCount,
        decimal TotalAmount,
        DateTime CreatedAt,
        DateTime? SubmittedAt,
        DateTime LastModifiedAt);
}
