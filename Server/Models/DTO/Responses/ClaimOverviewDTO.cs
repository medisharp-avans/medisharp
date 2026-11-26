using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    /// <summary>
    /// One row of the claims overview (GET /api/claims). A DTO, because the Claim entity points to the
    /// patient and the claim lines, and we only want to send what the overview shows.
    /// Init properties instead of a constructor, so EF Core can still filter and sort after the projection.
    /// </summary>
    public record ClaimOverviewDTO
    {
        public int Id { get; init; }
        public int PatientId { get; init; }
        public string PatientName { get; init; } = string.Empty;
        public string PolicyNumber { get; init; } = string.Empty;
        public ClaimStatus Status { get; init; }
        public int LineCount { get; init; }
        public decimal TotalAmount { get; init; }

        /// <summary>Full name of the hospital employee who reviewed the claim; null while it is not reviewed.</summary>
        public string? ReviewedByName { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? SubmittedAt { get; init; }
        public DateTime LastModifiedAt { get; init; }
    }
}
