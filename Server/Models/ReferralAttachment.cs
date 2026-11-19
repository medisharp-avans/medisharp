namespace Server.Models
{
    /// <summary>
    /// Metadata for an optional referral attachment. File content is stored separately.
    /// </summary>
    public class ReferralAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReferralId { get; set; }
        public Referral Referral { get; set; } = null!;

        public required string FileName { get; set; }
        public required string ContentType { get; set; }

        /// <summary>Internal file identifier assigned by the future upload service.</summary>
        public required string StorageKey { get; set; }

        public long SizeInBytes { get; set; }
    }
}
