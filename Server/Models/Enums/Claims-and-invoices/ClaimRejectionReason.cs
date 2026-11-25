namespace Server.Models.Enums
{
    /// <summary>Why the insurer rejected a claim line (the 'reden' field of the insurer API).</summary>
    public enum ClaimRejectionReason
    {
        /// <summary>Not covered by the policy conditions.</summary>
        PolicyCondition,

        /// <summary>The yearly maximum has been reached.</summary>
        MaximumReached,

        /// <summary>The policy number is unknown to the insurer.</summary>
        InvalidPolicyNumber
    }
}
