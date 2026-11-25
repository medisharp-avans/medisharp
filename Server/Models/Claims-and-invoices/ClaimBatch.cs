namespace Server.Models
{
    /// <summary>A batch of claims sent to the insurer in one go (table claim_batches).</summary>
    public class ClaimBatch
    {
        public int Id { get; set; }

        public int SentById { get; set; }
        public HospitalEmployee SentBy { get; set; } = null!;

        public DateTime SentAt { get; set; }

        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
