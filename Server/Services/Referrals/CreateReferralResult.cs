using Server.Contracts.Referrals;

namespace Server.Services.Referrals
{
    /// <summary>Contains either a saved referral's confirmation or validation errors without patient values.</summary>
    public class CreateReferralResult
    {
        public CreatedReferralResponse? Referral { get; init; }
        public Dictionary<string, string[]> Errors { get; init; } = [];
    }
}
