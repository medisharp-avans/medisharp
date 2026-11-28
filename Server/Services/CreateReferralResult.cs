using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;

namespace Server.Services
{
    /// <summary>Contains either a saved referral's confirmation or validation errors without patient values.</summary>
    public class CreateReferralResult
    {
        public CreatedReferralResponse? Referral { get; init; }
        public Dictionary<string, string[]> Errors { get; init; } = [];
    }
}
