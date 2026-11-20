namespace Server.Contracts.Referrals
{
    public class CreatedReferralResponse
    {
        public Guid Id { get; init; }
        public required string Status { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
