namespace Server.Models
{
    /// <summary>A hospital staff member with a user account (table hospital_employees). Reviews claims and sends claim batches.</summary>
    public class HospitalEmployee
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>Plain text for now; the EER has a departments lookup table that does not exist in code yet.</summary>
        public string Department { get; set; } = string.Empty;

        public ICollection<Claim> ReviewedClaims { get; set; } = new List<Claim>();
        public ICollection<ClaimBatch> SentClaimBatches { get; set; } = new List<ClaimBatch>();
    }
}
