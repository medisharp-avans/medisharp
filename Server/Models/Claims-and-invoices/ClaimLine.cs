using Server.Models.Enums;

namespace Server.Models
{
    /// <summary>One claimed appointment within a claim (table claim_lines). An appointment is claimed at most once.</summary>
    public class ClaimLine
    {
        public int Id { get; set; }

        public int ClaimId { get; set; }
        public Claim Claim { get; set; } = null!;

        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; } = null!;

        public decimal SubmittedAmount { get; set; }
        public decimal? ReimbursedAmount { get; set; }
        public string? RejectionReason { get; set; }
        public ClaimLineStatus Status { get; set; } = ClaimLineStatus.Pending;

        public DateTime? SubmittedAt { get; set; }

        /// <summary>How many times this line was sent to the insurer (resubmissions after a rejection count too).</summary>
        public int SubmissionCount { get; set; }
    }
}
