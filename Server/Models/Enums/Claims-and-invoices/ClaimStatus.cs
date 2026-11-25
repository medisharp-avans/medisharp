namespace Server.Models.Enums
{
    /// <summary>Lifecycle status of a claim (lookup table claim_statuses).</summary>
    public enum ClaimStatus
    {
        /// <summary>Created, waiting for a hospital employee to review it.</summary>
        ToReview,

        /// <summary>Reviewed and ready to be sent in the next batch.</summary>
        ReadyToSend,

        /// <summary>Handled by the insurer.</summary>
        Processed,

        /// <summary>Taken out of the automatic flow and handled by hand.</summary>
        HandledManually
    }
}
