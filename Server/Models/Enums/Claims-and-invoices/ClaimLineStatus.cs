namespace Server.Models.Enums
{
    /// <summary>Insurer outcome of a single claim line (lookup table claim_line_statuses).</summary>
    public enum ClaimLineStatus
    {
        /// <summary>Sent, no answer from the insurer yet.</summary>
        Pending,

        /// <summary>Fully reimbursed.</summary>
        Reimbursed,

        /// <summary>Partly reimbursed; the rest is invoiced to the patient.</summary>
        PartiallyReimbursed,

        /// <summary>Not reimbursed; see the rejection reason.</summary>
        Rejected
    }
}
