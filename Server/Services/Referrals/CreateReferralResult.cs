using Server.Contracts.Referrals;

namespace Server.Services.Referrals
{
    public class CreateReferralResult
    {
        public CreatedReferralResponse? Referral { get; init; }
        public Dictionary<string, string[]> Errors { get; init; } = [];
    }
}
