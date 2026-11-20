namespace Server.Contracts.Referrals
{
    /// <summary>Confirms creation without returning patient or medical information.</summary>
    public class CreatedReferralResponse
    {
        public Guid Id { get; init; }
        public required string Status { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
