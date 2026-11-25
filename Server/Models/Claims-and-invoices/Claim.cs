using Server.Models.Enums;

namespace Server.Models
{
    /// <summary>A claim for one patient that is submitted to the insurer (table claims).</summary>
    public class Claim
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int? BatchId { get; set; }
        public ClaimBatch? Batch { get; set; }

        /// <summary>The hospital employee who reviewed the claim, once reviewed.</summary>
        public int? ReviewedById { get; set; }
        public HospitalEmployee? ReviewedBy { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.ToReview;

        /// <summary>Copied by the server from the patient when the claim is created.</summary>
        public string PolicyNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ClaimLine> ClaimLines { get; set; } = new List<ClaimLine>();
    }
}
